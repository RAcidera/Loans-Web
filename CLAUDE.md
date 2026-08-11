# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A lending/loan-management system: fixed 60-day loan terms, flat 3%
interest, manual extensions, and a separate cash ledger for revolving
funds. Two halves in one repo:

- **Backend** (repo root `src/LoanManagementSystem.*`, `tests/*`): .NET 8
  Web API, Clean Architecture (Domain → Application → Infrastructure →
  Api), DDD aggregates, CQRS via MediatR, EF Core + SQL Server.
- **Frontend** (`src/loan-manager-admin-angular/`): standalone-component
  Angular 20 admin dashboard, its own Clean Architecture layering
  (domain/application/infrastructure/presentation), talking to the
  backend over HTTP.

Both READMEs (`README.md`, `src/loan-manager-admin-angular/README.md`)
and `TESTING.md` were written before the code was ever compiled/run and
before several pages existed — treat their prose on *design decisions* as
current, but not their tables of "what's built" or test-count claims.
Verify actual state by reading the code (e.g. `Controllers/`,
`presentation/`, `app.routes.ts`) rather than trusting those tables.

## Commands

### Backend (run from repo root, `LoanManagementSystem.sln`)

```bash
dotnet build                       # whole solution
dotnet test                        # all test projects (Domain, Application, Api.Tests) — no SQL Server needed, Api.Tests uses an in-memory Sqlite DB
dotnet test --filter "FullyQualifiedName~LoanTests"   # one test class
dotnet test --filter "FullyQualifiedName~LoanTests.Extend_PushesOutDueDate_AddsFee_MarksExtended"  # one test

dotnet run --project src/LoanManagementSystem.Api  # listens on http://localhost:5080, opens /swagger in Development
```

EF Core migrations (must be run from `src/LoanManagementSystem.Api` so the
startup project resolves the connection string; migrations live in
`LoanManagementSystem.Infrastructure`):

```bash
cd src/LoanManagementSystem.Api
dotnet ef migrations add <Name> --project ../LoanManagementSystem.Infrastructure --startup-project .
dotnet ef database update --project ../LoanManagementSystem.Infrastructure --startup-project .
```

**Always open the generated migration file before applying it.** EF's
auto-generated `defaultValue` for a new `NOT NULL` column backed by
`HasConversion<string>()` on an enum (the `CustomerStatus`/`UserStatus`
pattern used throughout this domain) comes out as `""`, not the enum's
default member name — applying it as-is leaves every pre-existing row
with a status the converter can't parse back into an enum, breaking reads
for existing data. Fix the `defaultValue` string to match the aggregate's
actual in-memory default before running `database update`.

If no SQL Server is reachable yet, `DbSeeder` falls back to
`Database.EnsureCreatedAsync()` (builds schema straight from the current
model, no migration history) — fine for a quick boot, but switch to real
migrations before evolving the schema further.

Connection string / JWT secret: `src/LoanManagementSystem.Api/appsettings.json`
(`ConnectionStrings:Default`, `Jwt:Secret`) — prefer `dotnet user-secrets`
over editing this file for local passwords.

### Frontend (run from `src/loan-manager-admin-angular/`)

```bash
npm install
ng serve                # http://localhost:4200, expects the API at environment.ts's apiBaseUrl (http://localhost:5080/api)
ng build                # production build; budget-exceeded is a pre-existing warning, not a regression signal by itself
ng test                 # Karma/Jasmine
```

No ESLint config in this project — there is no `ng lint` step.

Demo accounts seeded by `DbSeeder` on first run: `admin`/`Admin@12345`
(Admin role) and `staff`/`Staff@12345` (Staff role).

## Backend architecture

Dependency direction: `Api → Infrastructure → Application → Domain`, and
`Api → Application → Domain` directly for MediatR dispatch. `Domain` has
no project references besides the MediatR package (for the
`INotification` marker interface domain events extend).

- **Domain** (`LoanManagementSystem.Domain`) — aggregates (`Loan`,
  `Customer`, `User`, `CashLedgerEntry`) with enforced invariants (private
  setters, validation in factory methods/mutators throwing
  `DomainException`), value objects (`Money`, `InterestRate`), strongly-typed
  IDs (`readonly record struct XxxId(Guid Value)` with `.New()`/`.Parse()`),
  domain events, and repository *interfaces* only (`Domain/Repositories/`).
  Status-like fields follow one recurring pattern: an enum (`CustomerStatus`,
  `UserStatus`) + a private setter + a `Deactivate()`-style mutator, mapped
  via `HasConversion<string>()` in the EF configuration (see the migration
  gotcha above).
- **Application** (`LoanManagementSystem.Application`) — CQRS via MediatR:
  one file per command/query holding both the `IRequest<T>` record and its
  `IRequestHandler<TRequest,TResponse>`, grouped by aggregate
  (`Users/Commands/CreateUser/CreateUserCommand.cs`, etc.). Registered
  automatically via assembly scanning (`DependencyInjection.AddApplication`)
  — no manual wiring when adding a new command/query. DTOs live in
  `Common/DTOs/`, hand-written mapping extension methods (`ToDto()`) in
  `Common/Mappings/MappingExtensions.cs` — deliberately not AutoMapper.
  Two exception types map to specific HTTP statuses:
  `AuthenticationFailedException` (401) and `NotFoundException` (404);
  `DomainException` maps to 400. There's no "already exists" exception —
  handlers just throw `DomainException("X already taken.")` for that case.
- **Infrastructure** (`LoanManagementSystem.Infrastructure`) — `AppDbContext`,
  one `IEntityTypeConfiguration<T>` per aggregate in `Persistence/Configurations/`,
  concrete repositories (reads use `.AsNoTracking()`), `UnitOfWork`
  (`SaveChangesAsync` called explicitly by handlers after mutating via a
  repository's `Add`/loaded-and-mutated aggregate), `DbSeeder` (idempotent —
  checks whether `Customers` is non-empty before seeding), and
  `Pbkdf2PasswordHasher`/`JwtTokenGenerator`. New repository methods only
  need implementing here — the interface addition in Domain plus this
  implementation is the whole change; DI registration
  (`DependencyInjection.AddInfrastructure`) is per-*interface*, not per-method.
- **Api** (`LoanManagementSystem.Api`) — controllers are thin: build a
  command/query, `_mediator.Send(...)`, return. Authorization pattern used
  everywhere: class-level `[Authorize]` (any authenticated user) plus
  per-action `[Authorize(Roles = "Admin")]` overrides for privileged
  actions. `ExceptionHandlingMiddleware` centralizes exception → status
  mapping so controllers never need try/catch. JWT claims:
  `JwtRegisteredClaimNames.Sub` (user id, short-form — note inbound claim
  mapping can rename this on read, verify empirically if you rely on it),
  `ClaimTypes.Name` (username — safe to read via `User.Identity.Name`),
  `ClaimTypes.Role` (role, what `[Authorize(Roles=...)]` checks).

### The domain-event pattern worth understanding before touching Loans or CashLedger

`Loan` and `CashLedgerEntry` are separate aggregates with separate
repositories on purpose (the SRS treats funds tracking as a distinct
concern from loan receivables). `Loan.RecordPayment()` /
`Loan.Originate()` raise domain events (`PaymentRecordedDomainEvent`,
`LoanCreatedDomainEvent`); `AppDbContext.SaveChangesAsync` collects and
publishes them via MediatR after the triggering change commits; an
`EventHandlers/*Handler` in Application creates the corresponding
`CashLedgerEntry` through `ICashLedgerRepository` in its own,
separate `SaveChangesAsync`. `LoanExtendedDomainEvent` is raised but has
no ledger-touching handler (an extension adds a fee, doesn't move cash).
**Trade-off**: each event's effect is its own DB round trip, not the same
transaction as the change that raised it — acceptable at this scale, but
not atomic. If you need atomicity, wrap both in an explicit
`IDbContextTransaction`.

### Wire format

`Program.cs` configures camelCase JSON + `JsonStringEnumConverter`
globally, but in practice every enum-like field is pre-lowercased to a
plain string inside `MappingExtensions` (`Status.ToString().ToLowerInvariant()`)
before it ever reaches a DTO — so DTOs are plain records of strings/primitives,
shaped to match the Angular entity field-for-field. Follow that convention
for new DTOs rather than serializing a raw enum.

## Frontend architecture

```
presentation/   →   application/   →   domain/   ←   infrastructure/
 (Angular UI)      (use cases)      (entities,          (Http*Repository
                                      abstract ports)      implementations)
```

An inner layer never imports from an outer one. `app.config.ts` is the
composition root — the only place abstract repository ports (`LoanRepository`,
`CashLedgerRepository`, `ReportRepository`, `UserRepository`) get bound to
`Http*` implementations via `{ provide, useClass }`. A missing provider
here only surfaces as a runtime DI error the first time that page is
opened, not at `ng build` time.

- **One repository port per lifecycle boundary**, not one giant
  repository — `LoanRepository` covers Customers/Loans/Payments (the SRS
  groups those under one lifecycle), while `CashLedgerRepository`,
  `ReportRepository`, and `UserRepository` are each their own port for
  their own boundary. When adding a new concern, default to a new
  dedicated port rather than extending an unrelated one.
- **One use case class per operation** (`application/use-cases/`), each
  ~10 lines, `@Injectable({ providedIn: 'root' })`, injecting the abstract
  port and forwarding to one method on it. Components depend on use cases,
  never on a repository or `HttpClient` directly.
- **Standalone components everywhere** — no `NgModule`s. Each page/dialog
  component imports exactly the Material modules it needs.
- **Role gating is done inside components** (`*ngIf="authService.hasRole('admin')"`),
  not via route guards — `authGuard` only checks "is logged in" at the
  route level. `AuthService.hasRole()` reads a signal populated from the
  JWT-derived session in `localStorage`.
- **SCSS is per-component, deliberately duplicated**, not centralized —
  `.mono`, `.chip`/`.chip--<variant>`, `.table-card`, `.empty-hint` etc.
  are redefined in each page's own `.scss` file rather than pulled from a
  shared stylesheet (aside from CSS custom properties in `styles/tokens`).
  Follow the existing duplication rather than trying to factor it out.
  Dialog forms do share one partial, `presentation/_shared-dialog-form.scss`
  (`@use '../_shared-dialog-form' as *;`), for title/form/actions/error
  layout.
- **Inline error handling**: no snackbar/toast library anywhere in the
  app. Errors surface as an inline `<p>` bound to a component field,
  checked against `err.status` (see `login.component.ts` for the
  canonical example) — mirror this rather than introducing a new error UI
  pattern.
- Money, dates, and IDs render in `var(--lm-font-mono)` via a `.mono`
  class; headings use `var(--lm-font-display)`.
