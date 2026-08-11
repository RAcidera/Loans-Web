---
name: implement-settings-page
description: Implement the Settings page (Phase 3 "User Management" from loan-management-implementation-plan.md) — the nav item exists and links to /settings but the route is commented out and no SettingsComponent exists. Unlike the Loans page, this is full-stack work: the backend has zero user-management endpoints (only login), and there's no UserStatus/deactivate concept on the User aggregate yet. Use when the user asks to "build the settings page", "implement user management", "add the /settings route", or references the still-unbuilt Settings nav item.
---

# Implement the Settings page

## Re-verify before starting

This skill was written from a snapshot of the repo. Before trusting anything
below, re-grep for `SettingsComponent`, `UsersController`, `GetUsersQuery`,
and `CreateUserCommand` — someone may have already built part of this since
the skill was written, and you don't want to duplicate or clobber it.

Nav item already exists, unrouted:
`src/loan-manager-admin-angular/src/app/presentation/admin-shell/admin-shell.component.ts:51`
— `{ label: 'Settings', icon: 'tune', route: '/settings' },`

Route is commented out:
`src/loan-manager-admin-angular/src/app/app.routes.ts:26`
— `// { path: 'settings', component: SettingsComponent },`

No `SettingsComponent`, no settings service/model, and — critically, unlike
the Loans page — **no backend support at all** beyond login. This is a
from-scratch build on both sides, not an edit to something partial.

## Scope — quote the implementation plan, don't re-derive it

`loan-management-implementation-plan.md` (repo root), "Phase 3 — User
Management", is the authoritative spec for this feature. Read it in full
before starting; the load-bearing part:

> **Why here:** Phase 0 proved auth works; this phase is what makes auth
> *operable* rather than a fixed pair of demo accounts.
>
> | Capability | Implementation |
> |---|---|
> | List/create users (Admin only) | `GetUsersQuery`, `CreateUserCommand` — mirrors `CreateCustomerCommand` exactly |
> | Change password (self-service) | `ChangePasswordCommand` — requires current password verification via `IPasswordHasher.Verify()`, then `User.ChangePasswordHash()` |
> | Deactivate a user | `DeactivateUserCommand` — needs a `UserStatus` concept added to the `User` aggregate (currently has none — add it now, following the same pattern as `CustomerStatus`) |
> | `UsersController` | `GET/POST /api/users`, `PUT /api/users/{id}/password`, `POST /api/users/{id}/deactivate` — all Admin-only except password change (self, or Admin for anyone) |
>
> **Acceptance criteria:** Admin creates a new Staff user, that user can log
> in, Staff cannot see the user list, either role can change their own
> password.

Don't treat the exact route shapes above as gospel if a cleaner option
exists — see the self-password-change note under the controller section
below for one deliberate deviation this skill recommends.

## Backend — what already exists (reuse, don't rebuild)

- `src/LoanManagementSystem.Domain/Identity/User.cs` — aggregate root with
  `Username`, `PasswordHash`, `Role`, `CreatedAtUtc`. `Create(username,
  passwordHash, role)` (lines 31–39) and `ChangePasswordHash(newHash)`
  (lines 41–46) already implemented and guarded with `DomainException`.
  Only missing: a `Status` field and a `Deactivate()` method.
- `src/LoanManagementSystem.Domain/Identity/UserRole.cs` — `enum UserRole {
  Admin, Staff }`.
- `src/LoanManagementSystem.Application/Common/Security/IPasswordHasher.cs`
  + `src/LoanManagementSystem.Infrastructure/Security/Pbkdf2PasswordHasher.cs`
  — `Hash(password)` / `Verify(password, hash)`, already used by login.
  Reuse directly; do not touch.
- `AppDbContext.Users` DbSet and
  `Infrastructure/Persistence/Configurations/UserConfiguration.cs` already
  exist and are wired up (`AppDbContext.cs:23`). No new DbSet needed — only
  a new column once you add `Status`.
- `IUserRepository` → `UserRepository` is already registered in DI:
  `src/LoanManagementSystem.Infrastructure/DependencyInjection.cs:28`.
  Adding methods to the interface only requires implementing them in
  `Infrastructure/Repositories/UserRepository.cs` — no new DI wiring.
- `src/LoanManagementSystem.Infrastructure/Persistence/Seed/DbSeeder.cs:100–102`
  already seeds `admin`/`Admin@12345` and `staff`/`Staff@12345` via
  `User.Create(...)`. Don't touch this — once you add `Status` with a
  sensible default (`Active`) in `Create`, these seeded users come out
  active automatically.
- JWT claims already issued by
  `src/LoanManagementSystem.Infrastructure/Security/JwtTokenGenerator.cs:24–31`:
  `Sub` = user id, `ClaimTypes.Name` = username, `ClaimTypes.Role` = role
  string. `[Authorize(Roles = "Admin")]` already reads `ClaimTypes.Role`
  correctly (see the existing Admin-only actions below) — this is the same
  mechanism to reuse, not something to reinvent.
- Role-authorization pattern to copy verbatim:
  `src/LoanManagementSystem.Api/Controllers/CustomersController.cs` — class-level
  `[Authorize]` (line 15) plus per-action `[Authorize(Roles = "Admin")]`
  overrides (lines 45, 55).
- Exception → HTTP status mapping is centralized in
  `src/LoanManagementSystem.Api/Middleware/ExceptionHandlingMiddleware.cs:34–41`:
  `DomainException` → 400, `NotFoundException` → 404,
  `AuthenticationFailedException` → 401. **There is no "conflict/already
  exists" exception type** — for "username already taken", just throw
  `DomainException("Username already taken.")` from the command handler; it
  already maps to 400 with no new plumbing.

## Backend — what needs building

| File | Purpose |
|---|---|
| `Domain/Identity/UserStatus.cs` | New enum, mirrors `CustomerStatus` |
| `Domain/Identity/User.cs` (edit) | Add `Status` property + `Deactivate()` |
| `Infrastructure/Persistence/Configurations/UserConfiguration.cs` (edit) | Map the new `Status` column |
| New EF migration | `AddUserStatus` |
| `Domain/Repositories/IUserRepository.cs` (edit) | Add `GetByIdAsync`, `GetAllAsync` |
| `Infrastructure/Repositories/UserRepository.cs` (edit) | Implement the two new methods |
| `Application/Common/DTOs/UserDto.cs` | New DTO — **never include `PasswordHash`** |
| `Application/Common/Mappings/MappingExtensions.cs` (edit) | Add `User.ToDto()` |
| `Application/Users/Queries/GetUsers/GetUsersQuery.cs` | List users |
| `Application/Users/Commands/CreateUser/CreateUserCommand.cs` | Create a user |
| `Application/Users/Commands/ChangePassword/ChangePasswordCommand.cs` | Self-service password change |
| `Application/Users/Commands/DeactivateUser/DeactivateUserCommand.cs` | Deactivate a user |
| `Api/Controllers/UsersController.cs` | Wires the four commands/queries above |

### 1. `UserStatus` + `User.Deactivate()` — mirror `CustomerStatus` exactly

`Domain/Customers/CustomerStatus.cs` is the whole pattern to copy:

```csharp
namespace LoanManagementSystem.Domain.Identity;

public enum UserStatus
{
    Active,
    Inactive,
}
```

In `User.cs`: add `public UserStatus Status { get; private set; }`, set it
to `UserStatus.Active` inside the private constructor (alongside
`CreatedAtUtc = DateTime.UtcNow;` at line 28), and add:

```csharp
public void Deactivate()
{
    Status = UserStatus.Inactive;
}
```

Then update `UserConfiguration.cs` — add a property mapping mirroring the
existing `Role` conversion (lines 24–27):

```csharp
builder.Property(u => u.Status)
    .HasConversion<string>()
    .HasColumnName("status")
    .HasMaxLength(20);
```

Generate the migration from `src/LoanManagementSystem.Api` (exact command
pattern from `README.md:130–135`, which already explains why this repo
ships without pre-generated migrations — the author had no SDK to verify
them against):

```bash
cd src/LoanManagementSystem.Api
dotnet ef migrations add AddUserStatus \
  --project ../LoanManagementSystem.Infrastructure \
  --startup-project .
```

**Read the generated migration file before trusting it** — same caution
`README.md:122–127` gives for the very first migration applies here too.

**Judgment call — surface it, don't guess silently:** should
`LoginCommandHandler.Handle` (`Application/Auth/Commands/Login/LoginCommand.cs:30–39`)
also reject inactive users? The plan doesn't say explicitly, but a
"Deactivate a user" feature that doesn't actually block login is cosmetic.
Recommended: add a `Status` check alongside the existing password check at
line 34, throwing the same `AuthenticationFailedException` so a deactivated
account fails login exactly like a bad password (no information leak about
*why* it failed). Confirm this decision with whoever's reviewing rather than
silently picking one.

**Second judgment call:** can an Admin deactivate their own account? Not
addressed by the plan. Recommended: block it outright in
`DeactivateUserCommandHandler` (`if (target is the caller) throw
DomainException(...)`) — locking out the only admin account is a hard
footgun to recover from, and the extra check is cheap.

### 2. `IUserRepository` additions

Add, mirroring `ICustomerRepository`'s `GetByIdAsync`/`GetAllAsync` shape
and `CustomerRepository.cs:17–24`'s `AsNoTracking()`-on-reads convention
exactly:

```csharp
Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
Task<List<User>> GetAllAsync(CancellationToken ct = default);
```

Implement in `Infrastructure/Repositories/UserRepository.cs` the same way
`CustomerRepository.cs:17–24` does it (`FirstOrDefaultAsync` by id,
`AsNoTracking().OrderBy(...).ToListAsync()` for the list — order by
`Username` for a stable, predictable admin-facing list).

### 3. Four Application slices — mirror the Customers ones file-for-file

- **`UserDto`** — same shape convention as `CustomerDto.cs:7–15`
  (`UserId, Username, Role, Status, CreatedAt`, all strings — role/status
  lowercased via `.ToString().ToLowerInvariant()` in the mapping, matching
  how `LoginCommand.cs:38` already lowercases `Role` for the login
  response). **Do not add `PasswordHash` to this DTO under any
  circumstances** — it leaves the process in every `GET /api/users`
  response otherwise.
- **`ToDto()` mapping** — add to `MappingExtensions.cs` next to
  `CustomerDto`'s (`MappingExtensions.cs:19–27`).
- **`GetUsersQuery`/Handler** — mirror `GetCustomersQuery.cs` (the whole
  file is ~24 lines) exactly: no parameters, calls
  `_userRepository.GetAllAsync(ct)`, maps each to a DTO. Admin-only
  enforcement happens at the controller, not here.
- **`CreateUserCommand`/Handler** — mirror `CreateCustomerCommand.cs`, with
  two deviations from the copy-paste: (a) call
  `_userRepository.GetByUsernameAsync(username, ct)` first and throw
  `DomainException("Username already taken.")` if it returns non-null —
  don't rely on the DB's unique index (`UserConfiguration.cs:20`) to
  surface this as a clean 400; (b) hash the plaintext password via
  `_passwordHasher.Hash(request.Password)` *before* calling
  `User.Create(username, hash, role)` — the aggregate must never see a
  plaintext password.
- **`ChangePasswordCommand`/Handler** — takes `(UserId userId,
  string currentPassword, string newPassword)`. Load the user via the new
  `GetByIdAsync`, verify `_passwordHasher.Verify(currentPassword,
  user.PasswordHash)` and throw `AuthenticationFailedException` on mismatch
  (same class login already throws, so a wrong "current password" behaves
  identically to a failed login — consistent, and no new exception type
  needed), then `user.ChangePasswordHash(_passwordHasher.Hash(newPassword))`
  and save.
- **`DeactivateUserCommand`/Handler** — load via `GetByIdAsync`, call
  `user.Deactivate()`, save. Include the self-deactivation guard from the
  judgment call above.

### 4. `UsersController` — mirror `CustomersController`'s authorization shape

Class-level `[Authorize]`, per-action `[Authorize(Roles = "Admin")]`
overrides — copy `CustomersController.cs` lines 15, 45, 55 verbatim as the
pattern.

```
GET  /api/users               Admin only  — list
POST /api/users                Admin only  — create
PUT  /api/users/me/password    any authenticated user — changes the CALLER's own password
POST /api/users/{id}/deactivate  Admin only
```

**Deliberate deviation from the plan's literal `PUT
/api/users/{id}/password` self-or-admin route:** use `/api/users/me/password`
instead and resolve the target purely from the caller's own token —
`User.FindFirstValue(ClaimTypes.Name)` (matching how `LoginCommandHandler`
already looks users up by username, not id) or `GetByIdAsync` against the
`Sub` claim if you confirm how ASP.NET Core's JWT bearer handler maps that
claim in this project (`JwtTokenGenerator.cs:26` issues it as
`JwtRegisteredClaimNames.Sub`, but inbound claim-mapping can rename it —
verify empirically with a real request rather than assuming). This
sidesteps writing a separate "is this id me, or am I Admin" authorization
check entirely, since a self-service password change never needs to touch
anyone else's account. If you'd rather match the plan's literal route
shape instead, that's a reasonable call too — just don't leave the
self-or-admin check unwritten.

Controller action shape to copy, from `CustomersController.cs:46–51`
(`Create`):

```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
{
    var command = new CreateUserCommand(request.Username, request.Password, request.Role);
    var created = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetAll), created);
}
```

## Frontend — mirror `cash-funds`, the closest analog

Not `customers` or `loans`: those pages exist because a *page* for them was
missing on top of an already-complete backend (see
`.claude/skills/implement-loans-page/SKILL.md` for that pattern). Here the
closest structural analog is `cash-funds` — a single admin-gated action
button, a plain list/table, no row-click-to-navigate behavior to worry
about re-breaking (the loans skill's `$event.stopPropagation()` warning
doesn't apply here; the user list only needs an explicit per-row
"Deactivate" button, not a row-click dialog).

### `UserRepository` port — its own file, don't cram into `LoanRepository`

`domain/repositories/loan.repository.ts:7–11`'s own comment explains why
Customers/Loans/Payments share one port: the SRS groups those tables under
one lifecycle boundary. Users are a separate boundary — mirror how
`CashLedgerRepository`/`ReportRepository` each get their own dedicated
abstract class instead.

```ts
// domain/repositories/user.repository.ts
import { Observable } from 'rxjs';
import { AppUser } from '../entities/app-user.entity';
import { UserRole } from '../../application/auth/auth.service';

export abstract class UserRepository {
  abstract getUsers(): Observable<AppUser[]>;
  abstract createUser(username: string, password: string, role: UserRole): Observable<AppUser>;
  abstract changeMyPassword(currentPassword: string, newPassword: string): Observable<void>;
  abstract deactivateUser(userId: string): Observable<void>;
}
```

### `AppUser` entity — mirror `customer.entity.ts`, reuse the existing `UserRole` type

`application/auth/auth.service.ts:7` already exports `export type UserRole
= 'admin' | 'staff';`, and the backend already lowercases the role string
for the login response (`LoginCommand.cs:38`). Reuse that exact type for
the new DTO's role field instead of inventing a second, parallel
role-type — keep `UserDto` on the backend lowercasing `Role`/`Status` the
same way so the two line up with zero mapping code.

```ts
// domain/entities/app-user.entity.ts
import { UserRole } from '../../application/auth/auth.service';

export type UserStatus = 'active' | 'inactive';

export interface AppUser {
  userId: string;
  username: string;
  role: UserRole;
  status: UserStatus;
  createdAt: string;
}
```

(Named `AppUser`, not `User` — nothing in this codebase collides with that
name today, but `User` is generic enough to invite a future collision;
`AppUser` costs nothing and avoids it.)

### `HttpUserRepository` — mirror `HttpLoanRepository`'s customer methods

Same shape as `http-loan.repository.ts:26–40` (`this.baseUrl` from
`environment.apiBaseUrl`, typed `this.http.get`/`post`):

```ts
@Injectable()
export class HttpUserRepository extends UserRepository {
  private readonly baseUrl = environment.apiBaseUrl;
  constructor(private readonly http: HttpClient) { super(); }

  getUsers(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(`${this.baseUrl}/users`);
  }
  createUser(username: string, password: string, role: UserRole): Observable<AppUser> {
    return this.http.post<AppUser>(`${this.baseUrl}/users`, { username, password, role });
  }
  changeMyPassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/me/password`, { currentPassword, newPassword });
  }
  deactivateUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/deactivate`, {});
  }
}
```

### Register in `app.config.ts` — easy to forget, fails silently until runtime

Add the import and one line to the `providers` array
(`app.config.ts:49–51`, alongside the other three `useClass` lines):

```ts
{ provide: UserRepository, useClass: HttpUserRepository },
```

A missing provider here throws a DI error only when the Settings page is
first opened, not at `ng build` time — don't skip it and assume the build
passing means it's wired up.

### Use-cases — mirror `create-customer.use-case.ts`'s one-liner shape

Four small `@Injectable({ providedIn: 'root' })` wrappers
(`GetUsersUseCase`, `CreateUserUseCase`, `ChangePasswordUseCase`,
`DeactivateUserUseCase`), each ~10 lines, each just forwarding to the one
`UserRepository` method it wraps — copy `create-customer.use-case.ts`
verbatim as the template.

### `settings.component` — the two independent sections from the plan

Path matches the plan's own citation exactly:
`presentation/settings/settings.component.ts` + `.html` + `.scss`.
Standalone component, same `imports`/`templateUrl`/`styleUrls` shape as
`cash-funds.component.ts:30–36`.

Two independent sections in the template:

1. **User list — Admin only.** Wrap the *entire* section in
   `*ngIf="authService.hasRole('admin')"`, not just an add-button — per the
   plan's acceptance criteria, "Staff cannot see the user list" at all, not
   just the ability to add to it. Table with an inline "Deactivate" button
   per row (icon-button, same `mat-icon-button` idiom as the loans table's
   view-details action column) that calls `deactivateUser` and reloads.
2. **Change my password — both roles.** A small reactive form
   (`currentPassword`, `newPassword`, `confirmNewPassword` with a
   client-side cross-field validator that the last two match), visible
   unconditionally, calling `ChangePasswordUseCase`.

Route registration — uncomment and finish `app.routes.ts:26`:

```ts
{ path: 'settings', component: SettingsComponent },
```

plus the eager import at the top, matching every other route's pattern
(`app.routes.ts:2–9`).

### `add-user-dialog` — mirror `add-customer-dialog` exactly

Fields: `username` (required), `password` (required — consider
`Validators.minLength(8)`, since unlike the other "Add X" dialogs this one
creates a real login credential), `role` (`mat-select` with two static
`mat-option`s, Admin/Staff — a static list, not a fetched one, so mirror
the *shape* of `add-loan-dialog.component.html:5–9`'s `mat-select` but
without the `*ngFor`). Import the shared dialog styling the same one-line
way every other dialog does:
`add-customer-dialog.component.scss:1` — `@use '../_shared-dialog-form' as
*;`. Open it from `settings.component.ts` with `{ width: '480px', maxWidth:
'95vw' }` and the exact same `dialog.open(...).afterClosed().subscribe(result
=> result?.added && this.load())` idiom as `cash-funds.component.ts:77–84`.

## Verification

This feature touches auth, so verify more defensively than a typical page:

- `dotnet build` from repo root, then `dotnet test` — confirm the new
  migration/handlers compile and nothing in the existing Login/Customers
  suites regressed.
- Generate and **read** the `AddUserStatus` migration before trusting it
  (see the caution above — this repo's own README flags EF migrations as
  exactly the kind of generated code worth double-checking by hand).
- Direct HTTP round-trip (same style as `implement-loans-page`'s
  verification section — no browser needed for this part):
  1. `POST /api/auth/login` as `admin` → token.
  2. `POST /api/users` (Bearer admin token) to create a `staff2` account →
     confirm the response has no `passwordHash` field anywhere.
  3. `GET /api/users` (Bearer admin token) → confirm `staff2` appears.
  4. `POST /api/auth/login` as `staff2` → confirms the password/role
     round-tripped correctly through hashing.
  5. `PUT /api/users/me/password` (Bearer staff2 token) → change its own
     password, then log in again with the new password to confirm it took.
  6. `POST /api/users/{id}/deactivate` (Bearer admin token) on `staff2`,
     then retry step 4's login — confirm it's rejected *only if* you chose
     to wire the login-time `Status` check (judgment call above); if you
     didn't, confirm that decision was deliberate, not an oversight.
  7. `GET /api/users` as `staff2` (Bearer staff2 token) → confirm 403, not
     200 with data — this is the crux of the "Staff cannot see the user
     list" acceptance criterion.
- `ng build` clean.
- A real browser check — this project's standing rule for UI changes (see
  `enhance-mobile-responsive-design`'s skill notes) applies doubly here
  since this feature gates who can see what: log in as Admin, confirm the
  user list renders and Settings is reachable from the nav; log out, log
  in as Staff, confirm the user-list section is entirely absent but the
  change-password form still works; create a brand-new Staff user as Admin
  and log in as that exact new user to close the loop end-to-end.
