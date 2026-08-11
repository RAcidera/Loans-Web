# Loan Management System — Backend

.NET 8 Web API implementing the uploaded SRS: fixed 60-day loan terms, flat
3% interest, manual extensions, and a separate cash ledger for tracking
revolving funds. Clean Architecture (Domain → Application → Infrastructure →
Api) with DDD patterns throughout — aggregates with enforced invariants,
value objects, domain events used to keep the Loans and CashLedger
boundaries consistent, and a CQRS-style Application layer via MediatR.

**⚠️ I was not able to run `dotnet build` while writing this** — the
environment I built it in has no .NET SDK installed. Every file was checked
by hand for namespace consistency, property-name alignment across layers,
and correct EF Core API usage, but **please run `dotnet build` immediately
after unzipping, before doing anything else**, and treat any compiler
errors it surfaces as more trustworthy than anything in these comments.

## Testing

See **`TESTING.md`** for the full requirements traceability matrix — which
SRS requirement maps to which code and which automated test, and an honest
breakdown of what's actually been executed (only the Angular build) versus
written-and-reviewed-but-never-run (everything in this backend, including
its own test suite). Run `dotnet test` from this directory to verify for
yourself; the API integration tests need no SQL Server, only the .NET SDK.

## Authentication

Every endpoint except `POST /api/auth/login` requires a Bearer token.
Demo accounts (seeded by `DbSeeder`, change before deploying):

| Username | Password | Role | Can do |
|---|---|---|---|
| `admin` | `Admin@12345` | Admin | Everything |
| `staff` | `Staff@12345` | Staff | View everything, record payments — not loan creation/extension, customer creation, or cash ledger entries |

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@12345"}'
# -> { "token": "...", "expiresAtUtc": "...", "username": "admin", "role": "admin" }

curl http://localhost:5080/api/loans -H "Authorization: Bearer <token>"
```

## Layers

```
LoanManagementSystem.Domain           — entities, value objects, domain events, repository ports
LoanManagementSystem.Application      — CQRS commands/queries (MediatR), DTOs, event handlers
LoanManagementSystem.Infrastructure   — EF Core, SQL Server, repository implementations
LoanManagementSystem.Api              — controllers, Program.cs, CORS, Swagger
```

Dependency direction: `Api → Infrastructure → Application → Domain`, and
`Api → Application → Domain` directly for MediatR dispatch. `Domain` has
no project references of its own (only the MediatR package, for the
`INotification` marker interface domain events extend).

## The interesting design decision: domain events across two aggregates

The SRS says: *"Each payment automatically creates a payment_received entry
in the Cash_Ledger table."* But `Loan` and `CashLedgerEntry` are two
separate aggregates with two separate repositories (`ILoanRepository`,
`ICashLedgerRepository`) — on purpose, because the SRS itself treats funds
tracking as a distinct concern from loan receivables (see its own
"0. Cash Ledger / Funds Tracking" section header).

So how does recording a payment create a ledger entry without
`LoanRepository` depending on `CashLedgerRepository` (which would violate
the whole point of having two separate ports)?

`Loan.RecordPayment()` raises a `PaymentRecordedDomainEvent`.
`AppDbContext.SaveChangesAsync` collects that event after the `Loan` change
commits, then publishes it via MediatR. `PaymentRecordedEventHandler` (in
the Application layer) receives it and creates the `CashLedgerEntry` through
`ICashLedgerRepository` — a completely separate call, in a separate
`SaveChangesAsync`. Same pattern for `LoanCreatedDomainEvent` → the
`loan_release` entry. `LoanExtendedDomainEvent` is raised too, but has no
handler that touches the ledger, because an extension doesn't move cash —
it just adds a fee to what's owed.

This is the resolution to a gap that was explicitly left as a TODO comment
in the Angular mock repository this backend replaces — see
`MockLoanRepository.recordPayment()` in the frontend project for that
comment, if you want to compare the "before" state.

**Trade-off worth knowing**: each domain event's effect is its own database
round trip (its own `SaveChangesAsync`), not wrapped in the same transaction
as the change that raised it. For a system this size that's a reasonable
simplification. If you need the loan update and its ledger entry to commit
or roll back together atomically, wrap both in an explicit
`IDbContextTransaction` started before the first `SaveChangesAsync` and
committed after the event handler's `SaveChangesAsync` returns.

## Setup

### 1. Prerequisites
- .NET 8 SDK
- SQL Server (2019+) running locally or reachable
- EF Core CLI tools: `dotnet tool install --global dotnet-ef` (if not already installed)

### 2. Configure the connection string

Edit `src/LoanManagementSystem.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=loan_management_system;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

For SQL auth instead of Windows/integrated auth, use
`User Id=...;Password=...;TrustServerCertificate=True;` in place of
`Trusted_Connection=True;TrustServerCertificate=True;`. For local
development, prefer `dotnet user-secrets` over committing a real password
to `appsettings.json`:

```bash
cd src/LoanManagementSystem.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=loan_management_system;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 3. Create the database migration

**This is a step I could not run myself** (no SDK available), so there is
no `Migrations/` folder in this delivery — EF migrations are generated code
that must exactly mirror the fluent configuration in
`Infrastructure/Persistence/Configurations/`, and hand-writing that
correctly without being able to verify it against the real EF Core tooling
would be more likely to introduce a bug than to save you time. Generate it
yourself:

```bash
cd src/LoanManagementSystem.Api
dotnet ef migrations add InitialCreate \
  --project ../LoanManagementSystem.Infrastructure \
  --startup-project .
```

This reads the model from every `IEntityTypeConfiguration<T>` in
`Infrastructure/Persistence/Configurations/` and generates the migration
against it.

**If you'd rather not deal with migrations yet**: `DbSeeder` checks
`Database.GetMigrations()` and falls back to `Database.EnsureCreatedAsync()`
(builds the schema directly from the current model) when none exist — so
the app will actually boot and create tables against an empty SQL Server
database even without this step. Skip straight to step 5. The trade-off:
`EnsureCreated()` doesn't support incrementally evolving the schema the
way real migrations do, so switch to the real migration workflow above
before this goes anywhere near production.

### 4. Apply the migration (creates the database + tables)

```bash
dotnet ef database update \
  --project ../LoanManagementSystem.Infrastructure \
  --startup-project .
```

### 5. Run the API

```bash
dotnet run --project src/LoanManagementSystem.Api
```

On first run, `DbSeeder` populates the database with the same illustrative
data the Angular frontend's mock repositories used (5 customers, 6 loans in
various states, a cash ledger with an initial deposit) — see
`Infrastructure/Persistence/Seed/DbSeeder.cs`. It's idempotent: it checks
whether any customers already exist before seeding, so it's safe to leave
running against a database that already has real data.

The API listens on `http://localhost:5080` by default (see
`Properties/launchSettings.json`) and opens Swagger UI at `/swagger` in
Development.

### 6. Point the Angular frontend at it

See `../frontend/README.md` — in short, `app.config.ts` is already wired
to `HttpLoanRepository`/`HttpCashLedgerRepository`, and
`src/environments/environment.ts` already points at
`http://localhost:5080/api`. Run this backend first, then `ng serve`.

## API surface

| Method | Route | Backs |
|---|---|---|
| GET | `/api/customers` | `LoanRepository.getCustomers()` |
| GET | `/api/customers/{id}` | `LoanRepository.getCustomerById()` |
| GET | `/api/customers/{id}/loans` | `LoanRepository.getLoansByCustomer()` |
| POST | `/api/customers` | SRS 3.1 (not yet called by the built UI) |
| GET | `/api/loans` | `LoanRepository.getLoans()` |
| GET | `/api/loans/{id}` | `LoanRepository.getLoanById()` |
| GET | `/api/loans/{id}/detail` | Loan + extensions + payments in one call (convenience; Angular composes this client-side instead) |
| GET | `/api/loans/{id}/extensions` | `LoanRepository.getExtensions()` |
| GET | `/api/loans/{id}/payments` | `LoanRepository.getPayments()` |
| POST | `/api/loans` | SRS 3.2, originate a loan (not yet called by the built UI) |
| POST | `/api/loans/{id}/payments` | `LoanRepository.recordPayment()`, SRS 3.4 |
| POST | `/api/loans/{id}/extensions` | `LoanRepository.extendLoan()`, SRS 3.3 |
| GET | `/api/payments/recent?limit=` | `LoanRepository.getRecentPayments()` |
| GET | `/api/cash-funds/summary` | `CashLedgerRepository.getSummary()`, Formulas 1-5 |
| GET | `/api/cash-funds/ledger` | `CashLedgerRepository.getLedgerEntries()` |
| POST | `/api/cash-funds/ledger` | `CashLedgerRepository.addTransaction()` — owner deposit/withdrawal/expense only |

## Known gaps / things to check before treating this as production-ready

- **Change the demo credentials and JWT secret before deploying anywhere
  reachable.** `DbSeeder` creates `admin`/`Admin@12345` and
  `staff`/`Staff@12345` for local development. `appsettings.json`'s `Jwt:Secret`
  is a placeholder — replace it with a long random value (32+ characters)
  via `dotnet user-secrets` or an environment variable, not committed to
  source control.
- **Authentication exists but hasn't been run.** JWT auth, PBKDF2 password
  hashing, and role-based `[Authorize]` are implemented (see `TESTING.md`
  for exactly what's covered), but — like the rest of this backend — none
  of it has been executed in the environment it was written in. Run
  `dotnet test` before trusting it.
- **No input validation library.** Commands do minimal validation
  (`DomainException` for business-rule violations, e.g. negative amounts);
  there's no FluentValidation pipeline checking things like "is this a
  well-formed phone number." Add one if the API will face untrusted input.
- **`DateOnly` + SQL Server compatibility.** `StartDate`/`DueDate`/etc. use
  C#'s `DateOnly`, mapped to SQL Server's `date` type. This is supported by
  `Microsoft.EntityFrameworkCore.SqlServer` 8.x, but if `dotnet ef
  migrations add` fails on these properties, check the installed package
  version first.
- **`GetLoansQuery` mutates `Loan.Status` in memory without saving.** It
  calls `loan.RefreshOverdueStatus()` to compute the current status for
  display, but never calls `SaveChangesAsync` — the mutation exists only
  in that request's `DbContext` and is discarded when the scope ends.
  This is intentional (a query shouldn't have side effects), but means
  `Overdue` status is computed on every read rather than stored — fine at
  this scale, worth revisiting with a scheduled job if the loan volume
  grows large enough for that per-request computation to matter.
- **Single-database-round-trip-per-event**, as described above — revisit
  if you need atomic multi-aggregate transactions.

## Project layout

```
backend/
├── LoanManagementSystem.sln
└── src/
    ├── LoanManagementSystem.Domain/
    │   ├── Common/                  Entity, AggregateRoot, ValueObject, IDomainEvent, DomainException
    │   ├── ValueObjects/             Money, InterestRate
    │   ├── Customers/                Customer aggregate
    │   ├── Loans/                    Loan aggregate, LoanExtension, Payment, domain events
    │   ├── CashLedger/                CashLedgerEntry aggregate
    │   └── Repositories/              ICustomerRepository, ILoanRepository, ICashLedgerRepository, IUnitOfWork
    ├── LoanManagementSystem.Application/
    │   ├── Common/DTOs, Mappings, Exceptions
    │   ├── Customers/, Loans/, CashLedger/    Commands + Queries
    │   └── EventHandlers/             LoanCreatedEventHandler, PaymentRecordedEventHandler
    ├── LoanManagementSystem.Infrastructure/
    │   ├── Persistence/               AppDbContext, EF configurations, DbSeeder
    │   └── Repositories/               Concrete repository implementations, UnitOfWork
    └── LoanManagementSystem.Api/
        ├── Controllers/
        ├── Middleware/                 Exception → HTTP status mapping
        └── Program.cs
```
