---
name: redeploy-to-iis
description: Push code changes to an ALREADY-DEPLOYED IIS instance of this app (backend API + Angular frontend) without repeating first-time setup. Publishes/builds, then stops the app pools, overwrites site content in place, and restarts — leaving every live setting untouched (IIS sites/app pools/bindings, appsettings.Production.json's already-correct values, the Jwt:Secret IIS app setting, SQL Server permissions, hosts file entries). Use when the user asks to "redeploy to IIS", "push these changes to IIS", "update the IIS deployment", "sync changes to production", "deploy these changes" when production already exists, or after making code changes on a machine where /deploy-to-iis has already been run once. If IIS sites don't exist yet, or the user is setting this up for the first time, use /deploy-to-iis instead — this skill assumes that groundwork is already done and stable.
---

# Redeploy to IIS (content refresh only)

This is the lightweight, repeatable counterpart to `/deploy-to-iis`. That
skill covers *first-time* setup (installing IIS features, creating app
pools/sites/bindings, wiring CORS/JWT/DB config, granting SQL access).
This skill assumes all of that already happened and is live and correct
— its only job is: build the latest code, and get it onto the existing
sites without touching anything else.

## When NOT to use this skill

- No IIS site exists yet for this app anywhere on the target machine.
- `appsettings.Production.json` / `environment.prod.ts` don't exist or
  look like unfilled placeholders.
- The user explicitly wants to change a production setting (a different
  DB, a different CORS origin, a new JWT secret, a different hostname).

Any of those means `/deploy-to-iis` is the right skill, not this one —
this one is deliberately narrow so it can't accidentally redo (or undo)
one-time setup.

## Re-verify before starting

Confirm the assumption this skill rests on still holds — don't just
trust this file:

- `src/LoanManagementSystem.Api/appsettings.Production.json` exists and
  has real (not placeholder) `ConnectionStrings:Default` and
  `Cors:AllowedOrigins` values.
- `src/loan-manager-admin-angular/src/environments/environment.prod.ts`
  exists and has a real (not placeholder) `apiBaseUrl`.
- `src/loan-manager-admin-angular/angular.json`'s `production`
  configuration has a `fileReplacements` entry swapping in
  `environment.prod.ts` (otherwise a "production" build still embeds the
  dev API URL — silently, with no build error).

Read the two config files above at run time rather than hardcoding
hostnames in this skill — a future session may point them at different
infrastructure, and this skill should keep working either way.

## What this skill deliberately never touches

- IIS site/app pool creation or bindings (`New-Website`,
  `New-WebAppPool`) — sites are looked up, never created.
- The `Jwt__Secret` IIS-level environment variable on the API site — a
  content-only redeploy never touches IIS configuration, only site
  *files*, so this is safe by construction as long as step 3 below is
  followed (copy files, don't recreate the site).
- `C:\Windows\System32\drivers\etc\hosts`.
- SQL Server logins/permissions.
- Any installer (.NET Hosting Bundle, URL Rewrite Module, IIS Windows
  features).
- The *content* of `appsettings.Production.json` / `environment.prod.ts`
  — they get redeployed as committed in source (which should already
  match what's live), not edited. If they need to change, that's a
  deliberate task of its own, not something this skill decides.

## Steps

### 1. Build

```powershell
cd <repo root>
dotnet publish src/LoanManagementSystem.Api -c Release
```

Publishes to the default `src/LoanManagementSystem.Api/bin/Release/net8.0/publish`
(framework-dependent — no `--self-contained` needed, the target IIS box
already has the Hosting Bundle from the original deploy). Confirm
`web.config` and `appsettings.Production.json` landed in that output
folder — `dotnet publish` copies them automatically, but verify rather
than assume:

```powershell
Get-ChildItem src\LoanManagementSystem.Api\bin\Release\net8.0\publish -Filter "web.config"
Get-ChildItem src\LoanManagementSystem.Api\bin\Release\net8.0\publish -Filter "appsettings.Production.json"
```

```powershell
cd src/loan-manager-admin-angular
npm run build
```

`npm run build` = `ng build`, which already defaults to the `production`
configuration (`defaultConfiguration: "production"` in `angular.json`) —
no extra flag needed. Output lands in
`dist/loan-manager-admin-angular/browser/`. Sanity-check the bundle
actually embeds the production API URL, not a leftover dev one — this
catches a broken/missing `fileReplacements` wiring before it ships:

```powershell
$distPath = "dist\loan-manager-admin-angular\browser"
$apiHost = (Get-Content ..\..\src\LoanManagementSystem.Api\appsettings.Production.json | ConvertFrom-Json).Cors.AllowedOrigins[0]  # or read environment.prod.ts's apiBaseUrl host directly
Select-String -Path "$distPath\main-*.js" -Pattern "localhost:5080" -SimpleMatch -Quiet
# ^ should print nothing / $false. If it prints $true, fileReplacements isn't wired and the build shipped the dev API URL.
```

### 2. Elevation reality check

Both apps just need `dotnet`/`npm` (no admin rights). The actual
redeploy — stopping/starting IIS app pools and writing into
`C:\inetpub\...` — needs Administrator. If the current shell isn't
elevated (`([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)`
returns `$false`), don't attempt it directly — write the script below to
a file and hand it to the user to run in an elevated PowerShell window,
the same way this skill's sibling `/deploy-to-iis` handles it. If the
shell *is* already elevated, run it directly instead of handing off.

### 3. The redeploy script

Write this to a scratch `.ps1` file (see the encoding warning below
before doing so) with the real hostnames substituted in from step
"Re-verify before starting," or read dynamically at the top of the
script itself as shown:

```powershell
#Requires -RunAsAdministrator
Import-Module WebAdministration
$ErrorActionPreference = 'Stop'

# Derive host headers from the repo's own production config instead of
# hardcoding them, so this script stays correct if they ever change.
$repoRoot = 'D:\BitBucket\Loans-Web'   # adjust if the repo lives elsewhere
$prodAppsettings = Get-Content "$repoRoot\src\LoanManagementSystem.Api\appsettings.Production.json" | ConvertFrom-Json
$webHostHeader = ([Uri]$prodAppsettings.Cors.AllowedOrigins[0]).Host
$envProdContent = Get-Content "$repoRoot\src\loan-manager-admin-angular\src\environments\environment.prod.ts" -Raw
$apiHostHeader = ([Uri]([regex]::Match($envProdContent, "apiBaseUrl:\s*'([^']+)'").Groups[1].Value)).Host

$apiPublishSource = "$repoRoot\src\LoanManagementSystem.Api\bin\Release\net8.0\publish"
$webPublishSource = "$repoRoot\src\loan-manager-admin-angular\dist\loan-manager-admin-angular\browser"

if (-not (Test-Path $apiPublishSource)) { throw "Backend publish output not found at $apiPublishSource - run step 1 first." }
if (-not (Test-Path $webPublishSource)) { throw "Frontend build output not found at $webPublishSource - run step 1 first." }

function Find-SiteByHostHeader([string]$hostHeader) {
    Get-Website | Where-Object {
        $_.bindings.Collection | Where-Object { $_.bindingInformation -match ":$([regex]::Escape($hostHeader))$" }
    } | Select-Object -First 1
}

$apiSite = Find-SiteByHostHeader $apiHostHeader
$webSite = Find-SiteByHostHeader $webHostHeader

if (-not $apiSite) { throw "No IIS site found bound to host header '$apiHostHeader' - this skill only refreshes an EXISTING deployment. Run /deploy-to-iis first if this is a first-time setup." }
if (-not $webSite) { throw "No IIS site found bound to host header '$webHostHeader' - this skill only refreshes an EXISTING deployment. Run /deploy-to-iis first if this is a first-time setup." }

$apiPhysicalPath = $apiSite.physicalPath
$webPhysicalPath = $webSite.physicalPath
$apiPoolName = $apiSite.applicationPool
$webPoolName = $webSite.applicationPool

Write-Host "API site: $($apiSite.name) -> $apiPhysicalPath (pool: $apiPoolName)"
Write-Host "Web site: $($webSite.name) -> $webPhysicalPath (pool: $webPoolName)"

Write-Host "Stopping app pools to release file locks..."
Stop-WebAppPool -Name $apiPoolName -ErrorAction SilentlyContinue
Stop-WebAppPool -Name $webPoolName -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Copying backend..."
Copy-Item -Path "$apiPublishSource\*" -Destination $apiPhysicalPath -Recurse -Force
Write-Host "Copying frontend..."
Copy-Item -Path "$webPublishSource\*" -Destination $webPhysicalPath -Recurse -Force

Write-Host "Starting app pools back up..."
Start-WebAppPool -Name $apiPoolName -ErrorAction SilentlyContinue
Start-WebAppPool -Name $webPoolName -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Done. The API applies any new EF migrations automatically on this restart"
Write-Host "(EnsureSchemaAsync -> MigrateAsync in Program.cs) against the live production DB -"
Write-Host "no manual migration step needed, as long as the app pool identity still has the"
Write-Host "db_ddladmin permission granted during the original /deploy-to-iis run."
Write-Host ""
Write-Host "Verify: http://$apiHostHeader/api/... and http://$webHostHeader"
```

Nothing in this script calls `New-Website`, `New-WebAppPool`,
`Add-WebConfigurationProperty` (the JWT secret setter), or touches the
hosts file — it only looks up what already exists and overwrites files.

**Critical encoding gotcha** (this bit a real session — don't reintroduce
it): write the script file as **plain ASCII only**. No em-dashes (`—`),
curly quotes, or other non-ASCII punctuation in `Write-Host` strings or
comments — use a plain hyphen (`-`) instead. Windows PowerShell 5.1
reads a `.ps1` file without a byte-order mark using the system codepage,
not UTF-8; a multi-byte UTF-8 character like `—` gets silently
misdecoded into garbage bytes that break string-literal parsing
(`Unexpected token`, `The string is missing the terminator`). After
writing the file, verify before handing it off:

```powershell
$content = Get-Content -Raw -Encoding UTF8 <path-to-script>
$content.ToCharArray() | Where-Object { [int]$_ -gt 127 } | Select-Object -Unique
# should print nothing
```

### 4. Verify

Same shape as `/deploy-to-iis`'s verification, but note the *expected*
result differs on an already-live production system with real users:

- Hit the API's login endpoint. A clean `401 Invalid username or
  password` (not a 500 or 502.5) is a **success signal**, not a failure —
  it means the app started, connected to the DB, and applied any pending
  migration without error. It just means you don't know a real
  production account's password, which is expected (production doesn't
  have the dev seed's `admin`/`Admin@12345` — `DbSeeder`'s demo seed is
  gated behind `IsDevelopment()`). A 500/502.5 *is* a real failure —
  usually a migration permission issue (the app pool identity lost
  `db_ddladmin`) or the Hosting Bundle/ANCM module state changed.
- Fetch the frontend's `index.html`, extract the hashed `main-*.js`
  filename from it, then fetch that bundle and grep it for a specific
  string from the change just shipped (e.g. a new label added this
  session) to confirm the deployed bundle is actually the new one, not a
  stale cached copy.
- Ask the user to log in with a real production account and spot-check
  the actual pages that changed.
