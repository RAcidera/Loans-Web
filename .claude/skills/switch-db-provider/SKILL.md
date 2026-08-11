---
name: switch-db-provider
description: Convert the backend's EF Core database provider between MySQL (Pomelo) and Microsoft SQL Server, in either direction. Required input — destinationProvider, either "mysql" or "mssql" — since the skill has no way to infer which direction to go without it; ask the user if it wasn't passed in args. Use when the user asks to "convert mysql to mssql", "convert to sql server", "switch the database provider", "migrate off MySQL", or "move to SQL Server" (or the reverse direction back to MySQL).
---

# Switch DB provider (MySQL ⇄ MSSQL)

## Required input: `destinationProvider`

This skill needs one input before doing anything: `destinationProvider`,
either `mysql` or `mssql` (accept `sqlserver`/`ms sql`/`sql server` as
synonyms for `mssql`). If it wasn't passed in `args`, ask the user with
`AskUserQuestion` before touching any file — don't guess a direction.

Then detect the **current** provider yourself rather than trusting the
user's premise — grep `src/LoanManagementSystem.Infrastructure/DependencyInjection.cs`
for `UseMySql` vs `UseSqlServer`. If `destinationProvider` already matches
what's wired up, say so and stop; there's nothing to convert.

## Re-verify before starting

This skill was written from a snapshot of the repo (single-provider setup,
no existing abstraction for swapping providers). Before trusting the file
list below, re-grep for `UseMySql`, `UseSqlServer`, `Pomelo`, and
`UseMySqlIdentityColumn` — someone may have already partially migrated
this since the skill was written.

At the time of writing, MySQL (Pomelo) is the only provider wired up:

- `src/LoanManagementSystem.Infrastructure/DependencyInjection.cs:19-23` —
  `options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions => mySqlOptions.EnableRetryOnFailure())`.
- `src/LoanManagementSystem.Infrastructure/LoanManagementSystem.Infrastructure.csproj:22` —
  `<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />`.
- `src/LoanManagementSystem.Infrastructure/Persistence/AppDbContext.cs:35-43` —
  `OnModelCreating` conditionally calls `.UseMySqlIdentityColumn()` on
  `Loan.LoanNumber` for every provider except Sqlite (used only by
  `Api.Tests`).
- `src/LoanManagementSystem.Infrastructure/Persistence/Configurations/LoanConfiguration.cs:26-32` —
  comment explicitly says the AUTO_INCREMENT setup is "MySQL-only".
- Three existing migrations in
  `src/LoanManagementSystem.Infrastructure/Persistence/Migrations/`
  (`InitialCreate`, `AddLoanNumber`, `AddUserStatus`) plus
  `AppDbContextModelSnapshot.cs`.
- `src/LoanManagementSystem.Infrastructure/Persistence/Seed/DbSeeder.cs:22-30` —
  comment explains the generated migrations "bake MySQL-specific
  relational annotations (e.g. `ascii_general_ci` collation)" into the SQL,
  which is *why* Sqlite (integration tests) bypasses migrations entirely
  via `EnsureCreatedAsync()` instead of replaying them.
- `src/LoanManagementSystem.Api/appsettings.json:3` — MySQL-shaped
  connection string (`Server=...;Port=3306;...;User=...;Password=...;`).

That last point is the load-bearing fact for this whole skill: **the
existing migrations are provider-flavored SQL, not portable metadata.**
You cannot "convert" a migration in place — delete and regenerate from the
current model against the new provider instead.

## Conversion table — what changes per direction

| File | MySQL → MSSQL | MSSQL → MySQL |
|---|---|---|
| `LoanManagementSystem.Infrastructure.csproj` | Remove `Pomelo.EntityFrameworkCore.MySql`, add `Microsoft.EntityFrameworkCore.SqlServer` (match the `Microsoft.EntityFrameworkCore` version already pinned, `8.0.10`) | Remove `Microsoft.EntityFrameworkCore.SqlServer`, add back `Pomelo.EntityFrameworkCore.MySql` `8.0.2` |
| `DependencyInjection.cs` | `UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), o => o.EnableRetryOnFailure())` → `UseSqlServer(connectionString, o => o.EnableRetryOnFailure())` | reverse of the above |
| `AppDbContext.cs` (`OnModelCreating`) | `.UseMySqlIdentityColumn()` → `.UseIdentityColumn()` (SQL Server's generic identity-column extension, same namespace `Microsoft.EntityFrameworkCore`) | `.UseIdentityColumn()` → `.UseMySqlIdentityColumn()` |
| `Migrations/` folder | Delete all three migration files + `.Designer.cs` pairs + `AppDbContextModelSnapshot.cs`, regenerate `InitialCreate` fresh against the new provider | same, in reverse |
| `appsettings.json` connection string | `Server=localhost;Port=3306;Database=lending-db;User=root;Password=...;` → `Server=localhost;Database=lending-db;User Id=sa;Password=...;TrustServerCertificate=True;` | reverse — restore the `Port=3306;User=`/no `TrustServerCertificate` MySQL shape |
| `LoanConfiguration.cs:26-32` comment | Update — no longer "MySQL-only", it's now whichever provider `AppDbContext.cs` targets | same, reversed |
| `DbSeeder.cs:22-30` comment | Update — the collation annotation it describes is MySQL-specific; after conversion to SQL Server the new migrations won't have it, so re-verify whether Sqlite's `EnsureCreatedAsync()` bypass is still necessary or was only needed for that one annotation | re-add the MySQL-collation framing if reverting back |
| `CLAUDE.md` / `README.md` mentions of "EF Core + MySQL (Pomelo)" | Update the one-line project description in both | reverse |

Nothing in `LoanManagementSystem.Api`, `LoanManagementSystem.Domain`,
`LoanManagementSystem.Application`, or any test project references
Pomelo/MySql/SqlServer directly — confirmed by grep at skill-writing time.
Re-confirm with a fresh grep for `Pomelo|MySql|SqlServer` across
`src/` and `tests/` before finishing, in case that's changed.

## Steps

### 1. Swap the package reference

Edit `src/LoanManagementSystem.Infrastructure/LoanManagementSystem.Infrastructure.csproj`.
Going to MSSQL, replace the Pomelo block (including its explanatory
comment, lines 16-22 — that comment only makes sense when Pomelo is
actually referenced) with:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
```

Going to MySQL, restore:

```xml
<!--
  Pomelo, not Oracle's MySql.EntityFrameworkCore or MySql.Data — Pomelo
  is the community EF Core provider for MySQL/MariaDB with the widest
  compatibility with plain EF Core APIs (migrations, DateOnly, etc.)
  and no dependency on MySQL Connector/NET's own (much heavier) ORM.
-->
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.2" />
```

Then `dotnet restore`.

### 2. Update `DependencyInjection.cs`

To MSSQL:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
```

To MySQL:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));
```

(SQL Server doesn't need a `ServerVersion.AutoDetect` equivalent —
`UseSqlServer` doesn't take one. Don't add a stray unused parameter trying
to mirror the MySQL call shape.)

### 3. Update `AppDbContext.cs`'s identity-column call

`OnModelCreating` (lines 35-43) branches on `isSqlite` and otherwise calls
a provider-specific identity extension on `Loan.LoanNumber`. Swap just
that one method call:

```csharp
loan.Property(l => l.LoanNumber).ValueGeneratedOnAdd().UseIdentityColumn();   // MSSQL
loan.Property(l => l.LoanNumber).ValueGeneratedOnAdd().UseMySqlIdentityColumn(); // MySQL
```

`UseIdentityColumn()` is a plain `Microsoft.EntityFrameworkCore` extension
(no extra `using` needed); `UseMySqlIdentityColumn()` needs
`Pomelo.EntityFrameworkCore.MySql.Infrastructure` — check the `using`
block at the top of the file still matches whichever one you land on.

### 4. Delete and regenerate migrations

The existing migrations contain provider-baked SQL (see the collation
note above) — they will not apply against the new provider. Delete the
whole `Persistence/Migrations/` folder contents (all `.cs`/`.Designer.cs`
pairs and `AppDbContextModelSnapshot.cs`), then regenerate from the
current model, same command pattern as every other migration in this repo
(`README.md`, `CLAUDE.md`):

```bash
cd src/LoanManagementSystem.Api
dotnet ef migrations add InitialCreate --project ../LoanManagementSystem.Infrastructure --startup-project .
```

**Open the generated migration before applying it** — same standing rule
this repo has for every migration (`CLAUDE.md`'s EF migration gotcha,
[[ef_migration_enum_default_gotcha]]): a fresh `InitialCreate` has no
existing rows to break, but double-check the enum-backed columns
(`CustomerStatus`, `UserStatus`, `LoanStatus` — all `HasConversion<string>()`)
came out as `nvarchar`/`varchar` with the right max length rather than
some provider default, and that `decimal(12,2)`/`decimal(5,4)`/`date`
column types survived unchanged (both providers support these type names
natively, so they should pass through as-is — verify rather than assume).

Do **not** run `dotnet ef database update` yet if the target database
doesn't exist/isn't reachable — that's a live schema change against
whatever `ConnectionStrings:Default` currently points to. Confirm the
connection string (next step) and check with the user before applying.

### 5. Update the connection string

`src/LoanManagementSystem.Api/appsettings.json`. MySQL shape (current):

```
Server=localhost;Port=3306;Database=lending-db;User=root;Password=...;
```

SQL Server shape — SQL auth:

```
Server=localhost;Database=lending-db;User Id=sa;Password=...;TrustServerCertificate=True;
```

or Windows/integrated auth (common for local SQL Server Developer/Express installs):

```
Server=localhost;Database=lending-db;Trusted_Connection=True;TrustServerCertificate=True;
```

`TrustServerCertificate=True` is there because SQL Server 2022+ defaults
to requiring encryption, and a local dev instance usually doesn't have a
trusted cert — without it, `dotnet run`/`dotnet ef database update` fails
with a certificate-chain error, not a connection-refused error, which is
easy to misdiagnose as "wrong password."

Ask the user which auth mode and port/instance name they actually have
before hardcoding either shape — don't guess a password. Same file also
holds `Jwt:Secret`; touch nothing else in it.

### 6. Update the two "MySQL-only" comments

- `LoanConfiguration.cs:26-32` — the comment says the AUTO_INCREMENT setup
  is "MySQL-only"; once MSSQL is the live provider that's no longer true
  for what's *running*, but the comment's actual point (Sqlite has no
  equivalent) still holds — reword to say "database-generated, not
  supported the same way in Sqlite" rather than naming one specific
  non-Sqlite provider.
- `DbSeeder.cs:22-30` — this comment's specific claim (`ascii_general_ci`
  collation forces the Sqlite bypass) was true for the MySQL-generated
  migrations. After regenerating fresh migrations under the new provider,
  re-read the new migration's `Up()` method and confirm whether that
  specific annotation is still present in some form — if the new
  provider's migration is clean SQL Sqlite could theoretically replay,
  the comment's reasoning needs updating to whatever the actual blocker
  is now (there may still be one — e.g. SQL Server's `nvarchar` defaults —
  don't assume the bypass is safe to remove without checking).

### 7. Update the two doc mentions

`CLAUDE.md`'s "EF Core + MySQL (Pomelo)" (Project section) and
`README.md:50`'s equivalent line — update both to name the new provider so
they don't mislead the next reader. Don't touch anything else in either
file; per `CLAUDE.md`'s own instructions, their "what's built" tables are
already known-stale and out of scope here.

## Verification

- `dotnet build` from repo root — confirms the package swap and code
  changes compile.
- `dotnet test` — `Api.Tests` uses an in-memory Sqlite DB via
  `EnsureCreatedAsync()`, not the migrations, so this suite should pass
  regardless of which relational provider is wired up. If it doesn't,
  that's a real regression, not a pre-existing gap — investigate rather
  than assuming Sqlite is the exception.
- Read the regenerated `InitialCreate` migration in full (step 4) before
  trusting it, same standing rule as every other migration in this repo.
- Only if a real target database is reachable and the user has confirmed
  it's fine to modify: `dotnet ef database update` from
  `src/LoanManagementSystem.Api`, then boot the API
  (`dotnet run --project src/LoanManagementSystem.Api`) and confirm
  `DbSeeder` seeds cleanly (watch startup logs — it seeds only when
  `Customers` is empty) and `POST /api/auth/login` with the seeded
  `admin`/`Admin@12345` account returns a token.
- Final grep sweep for `Pomelo|MySql|SqlServer` across `src/` and `tests/`
  — confirm no stray reference to the old provider survived outside the
  files this skill intentionally reversed (e.g. a forgotten mention in a
  code comment elsewhere).
