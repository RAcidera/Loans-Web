---
name: deploy-to-iis
description: Deploy both halves of this repo to IIS on Windows — the .NET 8 Web API backend (src/LoanManagementSystem.Api) as an IIS site running under the ASP.NET Core Module, and the Angular 20 admin frontend (src/loan-manager-admin-angular) as a static IIS site with SPA URL-rewrite. Use when the user asks to "deploy to IIS", "host this on IIS", "publish to IIS", "set up IIS for the app", or asks how to get the backend/frontend running on a Windows server.
---

# Deploy to IIS (backend + frontend)

## Re-verify before starting

This skill was written from a snapshot of the repo (no existing IIS
scaffolding at all — confirmed by grep/glob for `web.config`, `Dockerfile`,
`deploy*.ps1`, `.github/workflows/`: zero hits). Before trusting any fact
below, re-check:

- `src/LoanManagementSystem.Api/Program.cs` for the current CORS policy —
  someone may have already made it configurable.
- `src/LoanManagementSystem.Api/appsettings*.json` — confirm whether
  `appsettings.Production.json` now exists.
- `src/loan-manager-admin-angular/angular.json` for `fileReplacements` —
  confirm whether `environment.prod.ts` is now actually wired up.
- Whether `web.config` files now exist under either app's publish output.

## System-level actions — confirm with the user before running

Installing IIS Windows features, the .NET Hosting Bundle, and the URL
Rewrite Module are machine-wide, hard-to-reverse changes (they affect
every site on the box, may require a reboot or `iisreset`). Per this
project's standing rule on risky actions, **state what you're about to
install/run and get explicit confirmation before executing** the
Prerequisites section below — don't just run it because the deploy was
requested. Building/publishing the apps and writing files inside the repo
is safe to do without re-asking.

## Current state (facts as surveyed)

**Backend** (`src/LoanManagementSystem.Api`):
- `.csproj` targets `net8.0`, SDK `Microsoft.NET.Sdk.Web` — this SDK
  auto-generates a `web.config` on `dotnet publish`, so no manual
  `<AspNetCoreHostingModel>` property is required to get IIS integration;
  it defaults to `InProcess` hosting via the ASP.NET Core Module (ANCM).
  No `Microsoft.AspNetCore.Server.IIS` package reference exists or is
  needed — that's implicit in the SDK.
- `Properties/launchSettings.json` — only a `http` profile,
  `applicationUrl: http://localhost:5080`, `ASPNETCORE_ENVIRONMENT:
  Development`. This is dev-only; IIS deployment doesn't use
  `launchSettings.json` at all — the site binding + `web.config`'s
  `<aspNetCore>` element determine the port/environment instead.
- `Program.cs:85-96` — CORS policy `"AngularDev"` hardcodes
  `.WithOrigins("http://localhost:4200")` with a comment already
  anticipating this needs to change for a deployed frontend origin.
  `app.UseCors(AngularCorsPolicy)` at line 122.
- `Program.cs:116-120` — Swagger is gated behind
  `app.Environment.IsDevelopment()`. IIS deployment normally runs with
  `ASPNETCORE_ENVIRONMENT=Production` (the ANCM default when unset), so
  Swagger UI will be off by default post-deploy — that's expected, not a
  bug to fix.
- `Program.cs`, right after `app.Build()` — `DbSeeder.SeedAsync(...)` and
  `DbSeeder.BackfillLoanLedgerAsync(...)` run **unconditionally on every
  startup**, in any environment. Seeding is idempotent (checks
  `Customers` is empty first), so this is safe to leave as-is, but it
  means the IIS app pool identity needs DB permission from the very first
  request, not just once.
- `Infrastructure/Persistence/Seed/DbSeeder.cs:38` uses
  `Database.EnsureCreatedAsync()`, not `Database.Migrate()` — schema is
  created from the current EF model on first connection, not replayed
  from migration files. Fine for a first deploy against an empty
  database; if the schema changes after that, `CLAUDE.md`'s standing
  migration guidance applies (switch to real migrations, and open the
  generated migration before applying it — the `HasConversion<string>()`
  enum-default gotcha).
- `appsettings.json` (full contents) — SQL Server connection string with
  **Windows/integrated auth** (`Trusted_Connection=True`), plus a
  **placeholder `Jwt:Secret`** literally committed as
  `"CHANGE_ME_TO_A_LONG_RANDOM_STRING_AT_LEAST_32_CHARS_BEFORE_DEPLOYING"`.
  No `appsettings.Production.json` exists yet.

**Frontend** (`src/loan-manager-admin-angular`):
- Angular 20, `@angular/build:application` builder (confirmed in
  `angular.json`) — production build output lands in
  `dist/loan-manager-admin-angular/browser/`, **not**
  `dist/loan-manager-admin-angular/` directly. Point IIS at the
  `browser` subfolder or the site will 404 on `index.html`.
  `npm run build` (`package.json`'s `"build": "ng build"`) already
  defaults to the `production` configuration
  (`"defaultConfiguration": "production"` in `angular.json`).
- **`angular.json` has no `fileReplacements` entry.** `environment.ts`
  (with `apiBaseUrl: 'http://localhost:5080/api'`) ships in the
  production bundle too — `environment.prod.ts` (with the placeholder
  `apiBaseUrl: 'https://api.yourdomain.example/api'`) exists but is dead
  code. This must be fixed before a production build is meaningful — see
  Step 2 below.
- `app.config.ts` uses `provideRouter(routes)` with no
  `withHashLocation()` — plain path-based routing (`/customers/:id`
  etc.). A browser refresh on a deep route will 404 on IIS unless a
  URL-rewrite-to-`index.html` rule is in place (IIS has no physical file
  at that path — this is the standard SPA-on-IIS problem, not specific
  to this app).

## Prerequisites (one-time machine setup)

Confirm with the user before running any of this (see above).

1. **IIS role** — Windows Features: `Web-Server` (IIS) with at least
   `Web-Static-Content`, `Web-Default-Doc`, `Web-Http-Errors`,
   `Web-Http-Logging`. From an elevated PowerShell:
   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-StaticContent,IIS-DefaultDocument,IIS-HttpErrors,IIS-HttpLogging,IIS-RequestFiltering,IIS-ManagementConsole
   ```
2. **.NET 8 Hosting Bundle** (ASP.NET Core Runtime + ANCM v2 module) —
   download and run the installer from Microsoft's official .NET
   downloads page (`dotnet-hosting-8.<latest>-win.exe`); this is the
   piece that lets IIS proxy requests into the published API's process.
   Run `iisreset` after install so IIS picks up the new module.
3. **URL Rewrite Module 2.1 for IIS** — not bundled with IIS by default;
   download/install from Microsoft's IIS URL Rewrite page. Required for
   the frontend's SPA fallback rule (Step 5).
4. Confirm `dotnet --list-sdks` shows an 8.x SDK on the machine doing the
   publish, and `node`/`npm` versions match `package.json`'s Angular 20
   toolchain for the build machine.

## One-time repo changes

### 1. Make CORS origins configuration-driven

Edit `src/LoanManagementSystem.Api/Program.cs` (around lines 85-96).
Replace the hardcoded origin with a config-read list, defaulting to the
existing dev origin so nothing breaks locally:

```csharp
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

This lets `appsettings.Production.json` (next step) supply the real
deployed frontend origin without touching code again on the next deploy.

### 2. Add `appsettings.Production.json`

New file, `src/LoanManagementSystem.Api/appsettings.Production.json`,
sibling to the existing `appsettings.json`/`appsettings.Development.json`.
Only override what differs from the base file — ASP.NET Core merges them:

```json
{
  "ConnectionStrings": {
    "Default": "Server=<PROD_SQL_SERVER>;Database=lending-db;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "<REPLACE_WITH_A_REAL_RANDOM_32+_CHAR_SECRET>"
  },
  "Cors": {
    "AllowedOrigins": ["https://<PROD_FRONTEND_ORIGIN>"]
  }
}
```

Ask the user for the real SQL Server host/instance and the frontend's
production origin (hostname + scheme + port) rather than guessing either
— both are environment-specific and wrong values fail silently or
insecurely (a wrong CORS origin just breaks the frontend with opaque
network errors; a wrong/weak JWT secret is a security issue, not a build
error). Don't commit a real production JWT secret to source control if
this repo's `appsettings.Production.json` will be checked in — prefer
`dotnet user-secrets` (already wired via `UserSecretsId` in the `.csproj`)
or an IIS-level environment variable override
(`web.config`'s `<environmentVariables>`, Step 4) instead, same guidance
`CLAUDE.md` already gives for the connection string/JWT secret locally.

### 3. Wire up `environment.prod.ts` for real

Edit `src/loan-manager-admin-angular/angular.json` — add a
`fileReplacements` array to the `production` configuration block (the one
containing the existing `budgets`/`outputHashing` entries):

```json
"fileReplacements": [
  {
    "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.prod.ts"
  }
]
```

Then edit `src/loan-manager-admin-angular/src/environments/environment.prod.ts`
to point at the real deployed API origin (ask the user; don't guess a
hostname):

```ts
export const environment = {
  production: true,
  apiBaseUrl: 'https://<PROD_API_ORIGIN>/api',
};
```

After this change, `ng build`/`npm run build` (production is already the
default configuration) will bundle the prod API URL instead of
`localhost:5080`. `ng serve` is unaffected — it doesn't apply
`fileReplacements` unless a non-default configuration is explicitly
passed, so local dev workflow (`environment.ts`, `localhost:5080`) stays
exactly as `CLAUDE.md` documents it.

### 4. Backend `web.config` overrides (environment + optional secret injection)

`dotnet publish` auto-generates a base `web.config` pointing ANCM at
`LoanManagementSystem.Api.dll`. If you need to force
`ASPNETCORE_ENVIRONMENT=Production` explicitly (normally the ANCM default
when unset, but explicit is safer than relying on that default) or inject
the JWT secret without committing it to `appsettings.Production.json`,
add an `<aspNetCore>` `<environmentVariables>` block. Rather than hand-edit
the generated file (it gets regenerated on every publish), create
`src/LoanManagementSystem.Api/web.config` in the project so `dotnet
publish` copies your version through as-is:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\LoanManagementSystem.Api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

Don't put the real JWT secret in this file if it's committed to source
control — use `Jwt__Secret` as an IIS Application Setting (IIS Manager →
site → Configuration Editor, or `appcmd`) instead, which ANCM surfaces as
an environment variable at that path (double underscore = config
section separator, standard ASP.NET Core convention).

### 5. Frontend `web.config` for SPA routing

New file, `src/loan-manager-admin-angular/web.config` — copy this into
the publish output folder alongside `index.html` (Step 7 below), or add
it to `angular.json`'s `assets` array so `ng build` copies it
automatically:

```json
"assets": [
  { "glob": "**/*", "input": "public" },
  { "glob": "web.config", "input": "src", "output": "/" }
]
```

(Check the existing `assets` array in `angular.json` first and add just
the `web.config` entry alongside whatever's already there — don't
replace the existing asset globs.)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="Angular Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
  </system.webServer>
</configuration>
```

This requires the URL Rewrite Module (Prerequisites step 3) — without it
IIS ignores the `<rewrite>` section silently rather than erroring, so if
deep-link refreshes still 404 after deploying, check the module is
actually installed before debugging further.

## Per-deployment steps

### 6. Publish the backend

```bash
dotnet publish src/LoanManagementSystem.Api -c Release -o C:\inetpub\LoanManagementSystem\Api
```

Framework-dependent by default (no `--self-contained`) — the target
machine needs the .NET 8 Hosting Bundle installed (Prerequisites step 2),
which it does. Confirm `appsettings.Production.json` and (if used)
`web.config` from steps 2/4 ended up in the output folder — `dotnet
publish` copies any `appsettings.*.json` present in the project
automatically, no extra step needed for that one.

### 7. Build the frontend

```bash
cd src/loan-manager-admin-angular
npm install
npm run build
```

Output lands in `dist/loan-manager-admin-angular/browser/`. Copy **that
subfolder's contents** (not the parent `dist/loan-manager-admin-angular/`
folder) to the IIS content path, e.g.:

```powershell
Copy-Item -Recurse -Force .\dist\loan-manager-admin-angular\browser\* C:\inetpub\LoanManagementSystem\Web\
```

If `web.config` wasn't wired into `angular.json`'s `assets` (Step 5),
copy it manually into `C:\inetpub\LoanManagementSystem\Web\` alongside
`index.html`.

### 8. Create the IIS app pool + site for the API

PowerShell, using the `WebAdministration` module (ships with IIS
management tools):

```powershell
Import-Module WebAdministration

New-WebAppPool -Name "LoanManagementSystem-Api"
Set-ItemProperty IIS:\AppPools\LoanManagementSystem-Api -Name managedRuntimeVersion -Value ""   # "No Managed Code" — ANCM handles the CLR, not IIS's managed pipeline

New-Website -Name "LoanManagementSystem-Api" -PhysicalPath "C:\inetpub\LoanManagementSystem\Api" -ApplicationPool "LoanManagementSystem-Api" -Port 5080
```

`managedRuntimeVersion = ""` is the "No Managed Code" setting — required
for any ASP.NET Core app pool since the CLR is hosted in-process by ANCM,
not by IIS's own managed pipeline; leaving a `v4.0` value here is a
common cause of a 500.19/502.5 error on first request.

Port 5080 mirrors the existing dev port so `apiBaseUrl` values stay
familiar, but confirm with the user which port/hostname/binding they
actually want on the target machine — that's environment-specific, not
something to assume.

### 9. Create the IIS site for the frontend

```powershell
New-WebAppPool -Name "LoanManagementSystem-Web"
New-Website -Name "LoanManagementSystem-Web" -PhysicalPath "C:\inetpub\LoanManagementSystem\Web" -ApplicationPool "LoanManagementSystem-Web" -Port 80
```

Static content doesn't need "No Managed Code" specifically, but there's
no reason to run it under a shared pool with the API either — keep them
isolated so an API process recycle doesn't affect the frontend and vice
versa.

### 10. Grant the API app pool identity SQL Server access

The connection string uses `Trusted_Connection=True` (Windows/integrated
auth) — **not** SQL auth. `ApplicationPoolIdentity` (the default for a
new app pool) runs as a virtual account named `IIS AppPool\<pool name>`,
e.g. `IIS AppPool\LoanManagementSystem-Api`. That exact account needs a
SQL Server login with permissions on `lending-db` (at minimum
`db_datareader`/`db_datawriter`/`db_ddladmin` for the first run, since
`EnsureCreatedAsync()` creates the schema) — this is the single most
common reason an otherwise-correct IIS deploy fails with a 500 error on
first request. If the SQL Server instance is remote (not on the same
box as IIS), a virtual app pool account can't authenticate cross-machine
at all — in that case switch the connection string to SQL auth
(`User Id=...;Password=...;`) instead, same as this repo's
`switch-db-provider` skill's MSSQL connection-string shape, minus
`Trusted_Connection`.

### 11. Restart / verify

```powershell
iisreset
```

## Verification

- `Invoke-WebRequest http://localhost:5080/swagger` from the IIS box —
  expect a 404 if `ASPNETCORE_ENVIRONMENT=Production` (Swagger is
  dev-only per `Program.cs:116-120`), or the Swagger page if you
  deliberately left it as Development for a staging box. Either way,
  confirm you got a real HTTP response, not a 502.5/500.19 (those
  indicate the Hosting Bundle or app pool "No Managed Code" setting is
  wrong).
- `Invoke-RestMethod -Method Post http://localhost:5080/api/auth/login -Body (@{username='admin'; password='Admin@12345'} | ConvertTo-Json) -ContentType 'application/json'`
  — confirms `DbSeeder` seeded correctly and the app can reach SQL Server
  under the app pool identity (Step 10).
- Open the frontend site in a browser, log in with the same seeded
  `admin`/`Admin@12345` account, confirm the network tab shows requests
  going to the production `apiBaseUrl` (Step 3) with no CORS errors in
  the console (confirms Step 1/2's `Cors:AllowedOrigins` matches the
  frontend's actual origin exactly, including scheme and port — CORS
  origin matching is exact-string, `http://` vs `https://` or a missing
  port mismatches silently).
- Navigate to a nested route (e.g. Customers list → a customer detail
  page) then hit browser refresh — confirms the URL Rewrite SPA rule
  (Step 5) is working; a 404 here means either the module isn't
  installed or `web.config` didn't land in the site's physical path.
- Check `C:\inetpub\LoanManagementSystem\Api\logs\stdout*.log` (if
  `stdoutLogEnabled` was turned on for debugging) for any startup
  exceptions — remember to set it back to `false` afterward, it's
  disabled by default in the Step 4 template for a reason (unbounded log
  growth otherwise).
