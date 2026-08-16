---
name: e2e-clean-db-validation
description: Run a thorough end-to-end regression validation of the entire lending application starting from a clean database — create real customers/loans/payments/extensions/cash transactions through the actual running app (API + UI), exercise edge cases (early settlement, overdue, bad loans, irregular payments, edits/deletes, double-submits), and reconcile Dashboard/Loans/Customers/Cash Ledger/Statement of Account/Interest Earned Report totals against each other and against hand-computed expected values. Use when the user asks to "test the whole app end to end", "run a full regression test from a clean database", "validate financial calculations across modules", or references the Final Reconciliation Checklist.
---

# End-to-end validation from a clean database

This is a long-running, multi-phase QA pass over the *whole* application, not a
quick smoke test. Treat it like `execute-plan`: work phase by phase, use
`TodoWrite` to track the 19 phases below plus the final checklist, and
checkpoint/report after each phase rather than trying to do everything in one
uninterrupted burst. If invoked to run (not just review), consider spawning it
as a background `Agent` given the volume of tool calls involved — but do the
Phase 0 database setup yourself first, synchronously, since later phases
depend on knowing exactly which database is in play.

**Re-verify before starting.** This skill encodes a snapshot of the codebase
(table names, formulas, known gaps). Re-grep anything you're about to rely on
rather than trusting it blindly — someone may have changed it since this was
written.

## Phase 0 — Get a clean database, safely

**Never run destructive SQL against the shared dev database the user has been
building up all session (the one with "Ana Villanueva", "Maria Santos", etc.)
without asking first.** Loans/customers created across many prior sessions
live there. Instead, point the app at a **disposable, separate** database for
this validation run:

1. Find the current connection string (`ConnectionStrings:Default` in
   `src/LoanManagementSystem.Api/appsettings.json`, or via
   `dotnet user-secrets list --project src/LoanManagementSystem.Api` if
   secrets are used instead). Copy it and change only the `Database=` (or
   `Initial Catalog=`) segment to something obviously disposable, e.g.
   `LoanManagementSystemE2E`.
2. Set it for the duration of this session via an environment variable
   (PowerShell), not by editing `appsettings.json`:
   ```powershell
   $env:ConnectionStrings__Default = "Server=...;Database=LoanManagementSystemE2E;..."
   ```
   (Double underscore is ASP.NET Core's config-nesting convention — this
   overrides the JSON value without touching the file.) Every `dotnet ef`
   and `dotnet run` command in this same PowerShell session will now target
   the E2E database instead of the real one.
3. From `src/LoanManagementSystem.Api`, run:
   ```powershell
   dotnet ef database update --project ../LoanManagementSystem.Infrastructure --startup-project .
   ```
   This creates the E2E database fresh, with the full migration history
   applied — this is a *real* migrated schema, not `EnsureCreatedAsync`'s
   fallback, so it exercises the same path production would.
4. Start the app once (`dotnet run --project src/LoanManagementSystem.Api
   --urls http://localhost:5080`, same env var still set) and wait for it to
   report "Now listening on...". `DbSeeder.SeedAsync` runs automatically in
   Development and — because the new database's `customers` table is
   empty — seeds its full demo dataset (5 customers, 6 loans, cash ledger
   entries, **and** the `admin`/`staff` login accounts) in one pass, gated by
   a single `Customers.AnyAsync()` check.
5. Wipe just the seeded *business* data, leaving `users` alone (you need
   `admin`/`Admin@12345` to log in). In FK-safe order (children before
   parents — verify these table names are still current via
   `Infrastructure/Persistence/Configurations/*Configuration.cs`'s
   `builder.ToTable(...)` calls before running):
   ```sql
   DELETE FROM payments;
   DELETE FROM loan_ledger;
   DELETE FROM loan_audit_log;
   DELETE FROM loan_extensions;
   DELETE FROM loan_documents;
   DELETE FROM loans;
   DELETE FROM customer_documents;
   DELETE FROM customers;
   DELETE FROM cash_ledger;
   ```
   Run this via whatever SQL client is available (`sqlcmd`, Azure Data
   Studio, SSMS, the `Invoke-Sqlcmd` PowerShell module if installed). Ask
   the user which they have if none is obviously available rather than
   guessing at a tool that might not be installed.

**Critical gotcha — do not restart the API process after this point until the
whole validation session is done.** `DbSeeder.SeedAsync` re-runs on every
startup and only skips reseeding if `customers` is non-empty. Once you've
wiped `customers` to zero, restarting the API will trigger a full reseed —
re-adding the 5 demo customers/loans/cash-ledger AND attempting to
`AddRange(admin, staff)` again, which will throw on the `users` table's
unique username constraint and crash startup. If you genuinely need to
restart mid-session (code change, crash recovery), either create at least
one throwaway customer via the API first (keeps `customers` non-empty) or
set `$env:ASPNETCORE_ENVIRONMENT = "Staging"` (anything other than
`Development`) before restarting — `SeedAsync` is wrapped in an
`IsDevelopment()` check and won't run at all outside it (Swagger won't
auto-open either, which is an acceptable tradeoff here).

Confirm the reset worked: `GET /api/customers` and `GET /api/loans` (with an
admin bearer token — `POST /api/auth/login` first) should both return empty
arrays.

## Verification approach: hybrid API + UI

Don't screen-scrape everything and don't skip the UI entirely — use whichever
is faster and more precise for each check, matching how this session's own
feature work was verified:

- **Creating test data**: prefer direct HTTP calls (PowerShell
  `Invoke-RestMethod` or a small Node/Playwright `request` context) for
  precise control over dates/amounts/terms — e.g. backdating a loan's
  `startDate` to manufacture an overdue loan is far easier via
  `POST /api/loans` than fighting a date picker. Reserve UI clicks for
  things that are inherently UI behavior: dialogs, filters, pagination,
  exports, empty states, double-submit races.
- **Reconciliation math**: read the real formulas out of the code you
  already know (or re-grep) rather than re-deriving them from scratch —
  `InterestCalculationService.cs` (daily accrual: `dailyInterest =
  contractAmount / termDays`, unrounded; `earned = Min(contractAmount,
  dailyInterest * elapsedDays)`), `GetDashboardSummaryQuery.cs`'s
  `BuildReceivablesBreakdown` (Current/Overdue/BadLoan buckets are each
  loan's `Balance`; Paid bucket is `TotalPaid` — these are deliberately
  different bases, already a once-fixed bug, worth re-checking), and
  `GetCashSummaryQuery.cs` (`CashOnHand = Σ SignedAmount` over every ledger
  entry). Hand-compute the expected number, then compare against what the
  screen/API/export shows — don't just eyeball that "a number appears."
- **Browser checks**: reuse this session's established Playwright pattern —
  launch headless Chromium, log in via the real form (`page.fill` on
  `input[formcontrolname="username"/"password"]`), `waitForURL` for
  post-login redirect (not a fixed timeout), screenshot key states, read
  `getBoundingClientRect`/`getComputedStyle` for layout claims, and always
  check `page.on('console')`/`page.on('pageerror')` for swallowed errors.
- Keep every scratch script/screenshot in the scratchpad directory, not the
  repo.

## Known-going-in findings (verify still true, log if so)

Two confirmed gaps found by reading the code, not yet observed live — treat
these as *expected* defects to reproduce and confirm, not things to discover
from scratch:

1. **`DeletePaymentCommandHandler` never reverses the mirrored
   `CashLedgerEntry`.** `Loan.DeletePayment` only rolls back
   `TotalPaid`/`Balance` on the loan itself; it raises no domain event, so
   the `payment_received` cash-ledger row from when the payment was
   recorded is never removed or adjusted. Expect: after deleting a payment,
   the loan's Balance is correct, but Cash on Hand (Dashboard and
   Transactions page) stays overstated by that payment's amount
   indefinitely, and the stale ledger row still shows a `ReferenceId`
   pointing at a payment that no longer exists — an orphaned financial
   transaction, directly hitting the "no orphaned financial transactions"
   and "deleting recalculates affected data" checklist items.
2. **Same gap for `LoanLedgerEntry` on both deleted payments and deleted
   extensions.** `DeleteExtensionCommandHandler`/`DeletePaymentCommandHandler`
   correctly adjust the loan's own fields but never remove the
   corresponding row from `loan_ledger`. `DbSeeder.BackfillLoanLedgerAsync`
   won't fix this either — it only *adds* missing entries for loans with
   zero ledger rows, it doesn't reconcile existing ones. Expect: a loan's
   Ledger tab keeps showing a payment/extension that was deleted, with a
   running balance column that no longer matches the loan's real balance
   from that point forward.

Confirm both still reproduce (they may have been fixed since this was
written) and log them under Phase 15 either way.

## Phases 1–19

Turn each of these into a `TodoWrite` item before starting. For every phase,
capture: what you did (with concrete IDs/amounts/dates so it's reproducible),
what you expected, what you observed, and pass/fail.

**Phase 1 — Application startup.** `dotnet build` clean. `dotnet run` starts
without exceptions and logs "Now listening on...". `/swagger` reachable
(Development only). Frontend `ng serve` loads `/login` with zero console
errors.

**Phase 2 — Empty-state behavior.** Log in as `admin`/`Admin@12345`. Visit
Dashboard, Customers, Loans, Transactions, Reports > Interest Earned — every
KPI should show ₱0.00/0 (not blank, not an error, not `NaN`/`Infinity`),
every grid should show its "No X yet" empty-hint, and the Interest Earned
Report's monthly bar chart should render 24 zero-height bars, not crash
(check `barChartMax`'s `Math.max(1, ...)` floor guards against a divide-by-
zero when every value is 0). Zero console/network errors throughout.

**Phase 3 — Create test customers.** Create at least 4, covering different
`borrowerType`/`nicknameAlias`/notes combinations, via the UI dialog (to also
verify the dialog itself). Note each `CustomerId`/`CustomerCode`. Confirm the
Customers list KPI counts update to match.

**Phase 4 — Loans with different terms.** Create at minimum:
- A 30-day loan (`paymentTermsMonths: 1`).
- A default 60-day loan (`paymentTermsMonths: 2`).
- A longer custom-term loan (e.g. `paymentTermsMonths: 3`).
- A loan with a manually overridden `interestAmount` (different from
  `principal * rate`) — this is what later makes the Interest Earned
  Report's Adjustment column non-zero.

For each, record Principal, InterestRate, TotalInterest, StartDate, DueDate —
this is your hand-computed baseline for Phase 14.

**Phase 5 — Irregular payments.** On one loan, record several small partial
payments on different dates via different `paymentMethod`s. On another,
settle the full balance in one lump sum. After each: confirm `Balance =
TotalAmountDue - TotalPaid` exactly, confirm a `payment_received` cash-ledger
row appeared automatically (Transactions page or `GET
/api/cash-funds/ledger/page`) with the right amount/date, and confirm the
Loan Detail page's Ledger tab running balance matches.

**Phase 6 — Loan extensions.** Extend one loan once, then extend it *again*
(two extensions on the same loan) — this specifically exercises the Interest
Earned Report's per-extension breakdown (each must show as its own card in
the drill-down dialog, in `Extend()` call order). Confirm `DueDate` pushes
out by exactly the extension's days each time, `TotalExtensionCharges`/
`TotalAmountDue`/`Balance` update, and `Status` becomes `Extended`.

**Phase 7 — Early settlement.** Pay a loan's full balance well before its
due date, then use Edit Loan to reduce `TotalInterest` below the rate-based
amount (simulating a negotiated discount). Confirm Status → `Paid`, Balance
→ 0. Flag this loan's ID for Phase 14 — its Interest Earned Report row should
show a negative Adjustment and a Final Earned capped at the *reduced* amount,
never the original.

**Phase 8 — Overdue loans.** Create a loan with `startDate` far enough in
the past that its term has elapsed with no extension (e.g. 70+ days ago on a
60-day term). Confirm the Loans list shows `Overdue` (computed at read time
by `RefreshOverdueStatus`, not stored). Confirm Dashboard's Overdue count
includes it. Flag this loan for Phase 14 — its earned interest must cap at
exactly its contract interest, never accrue past maturity (spec's core
invariant, already unit-tested in isolation by
`InterestCalculationServiceTests` — this phase re-proves it through the real
app end to end).

**Phase 9 — Bad-loan classification.** Use the Loan Details page's Change
Classification action to mark a loan `Bad Loan`. Confirm the badge updates,
Dashboard's Bad Loan Receivables KPI includes its Balance, and the
Receivables Breakdown donut's Bad Loan segment reflects it. Confirm the
Interest Earned Report can still show/filter this loan by Classification
(bad-loan status must not erase previously earned interest — spec
requirement, not just a UI nicety).

**Phase 10 — Customer/loan documents.** Upload a file to a customer and a
file to a loan (JPG/PNG/PDF). Confirm both appear in their respective
Documents tabs with correct size/name, download each back and confirm it
matches, then delete both and confirm they disappear.

**Phase 11 — Cash ledger.** Via the Transactions page, add one of each manual
type: `owner_deposit`, `owner_withdrawal`, `expense`, and an `adjustment` in
*both* directions (increases and decreases cash — the direction toggle only
appears for Adjustment). After each, confirm Cash on Hand updates by exactly
the signed amount. Edit one manual transaction's amount and confirm the
grid's Running Balance column recomputes correctly for every row from that
point on (it's computed fresh on every read, not stored — this should just
work, but confirm it does). Delete one manual transaction and confirm
totals/footer adjust. Then, via direct API call (bypassing the UI, which
hides the option), attempt `PUT`/`DELETE` on an automatic
(`payment_received`/`loan_release`) ledger entry's ID and confirm the backend
rejects it with 400, not 200 — this is a server-side guard
(`CashLedgerEntry.IsAutomatic`), not just a UI restriction, and should be
proven as such.

**Phase 12 — Dashboard totals.** Cross-check every KPI against an
independent hand-sum: Gross Receivables (sum of `Balance` for
Active/Extended/Overdue loans) matches the Loans list's own footer total for
that same filter; Collectible Receivables = Gross − Bad Loan; the
Receivables Breakdown donut's Current+Overdue+BadLoan segments must sum to
*exactly* Gross Receivables (previously-fixed bug — a strong regression
check); Cash on Hand matches the Transactions page's figure exactly; Recent
Loans/Payments feeds show the true 5 most recent in correct order; Loans Due
This Week matches a manual count of loans due within 7 days.

**Phase 13 — Statement of Account.** Generate the SOA PDF for the
multi-extension loan from Phase 6. Confirm every header figure matches the
Loan Detail page's own display exactly, the Payment History table's running
balance decreases correctly row by row, the *final* row's running balance
equals the loan's current `Balance`, and the Extension History table shows
both extensions with correct charges/new-due-dates.

**Phase 14 — Interest Earned Report.** For every flagged loan (Phases 4, 6,
7, 8), hand-compute expected `ContractInterest`/`ExtensionInterest`/
`EarnedBeforePeriod`/`EarnedThisPeriod`/`TotalEarned`/`Adjustment`/
`FinalEarned` using the exact formula in `InterestCalculationService.cs`,
for a chosen `[fromDate,toDate]` window, and compare row-by-row against
`GET /api/reports/interest-earned/page`. Confirm the grid's footer totals
equal the sum of the visible rows. Confirm the Overview endpoint's summary
KPIs equal your hand-summed Original/Extension/Adjustment totals. Open the
drill-down dialog for the two-extension loan and confirm exactly 3 period
cards (Original + Extension #1 + Extension #2) with correct per-period math.
Export both Excel and PDF and spot-check at least 2 rows in each against the
on-screen grid — they must match exactly (same calculation, per spec, not
independently re-derived in export code). For the fully-matured Phase 4/8
loans, sum `EarnedThisPeriod` across every month from origination through
maturity (via repeated Overview calls with monthly windows, or the monthly
chart) — the total must equal exactly the loan's (adjusted) contract
interest, with **zero** rounding drift, even for a term that doesn't divide
evenly (e.g. a 3-month/90-day loan) — this is the live, whole-app version of
the `FinalDayReconciliation_NoRoundingDrift` unit test.

**Phase 15 — Edit/delete scenarios.** Edit a Loan's principal/interest/dates
via Edit Loan; confirm `TotalAmountDue`/`Balance` recompute and a
`LoanAuditLogEntry` (Action=Edited) appears in the Audit Log tab. Edit a
Payment; confirm the mirrored `CashLedgerEntry` and `LoanLedgerEntry` update
via `Revise()` (this path *is* wired correctly — contrast with delete,
below). Delete a Payment and a deleted-loan's Extension; confirm/reproduce
the two known gaps above (orphaned `cash_ledger`/`loan_ledger` rows) and log
them as defects with exact IDs and before/after numbers if still present.
Test rapid double-clicking the submit button on Add Payment, Add Loan, and
Add Transaction dialogs; confirm exactly one record is created each time
(the dialogs set `submitting = true` and disable the button — confirm that
guard actually holds under a genuine double-click, don't just trust the
pattern).

**Phase 16 — Filters/search.** On Loans, Customers, Transactions, and
Interest Earned Report: exercise every filter (status, classification, date
ranges, search text, interest type) individually and in combination, confirm
results match expectations, confirm Clear resets everything and reloads
unfiltered.

**Phase 17 — Pagination/export.** Ensure enough rows exist (15–25+) on
Loans/Customers/Transactions/Interest Earned Report to force multiple pages.
Test next/prev/first/last and every page-size option; changing page size
must reset to page 0 without erroring. Export from every page that offers
it (CSV/XLSX/PDF as applicable) and confirm row counts and a few spot-checked
values match the currently-filtered on-screen totals, not the unfiltered
whole table.

**Phase 18 — Reconciliation checks.** Work through the Final Reconciliation
Checklist below as a discrete pass, citing the specific phase/evidence each
line rests on rather than re-testing from scratch.

**Phase 19 — Document defects.** Don't wait until the end to write these up —
log each as it's found, in the format below, and roll them into one final
report.

## Final Reconciliation Checklist

For each, state pass/fail and the evidence (which phase, which two numbers
you compared):

- [ ] Customer counts correct (Phase 3 vs Customers list KPI)
- [ ] Customer outstanding balances reconcile (Customer Profile sum of
      loans' `Balance` vs Customers list grid's Outstanding Balance column)
- [ ] Loan balances reconcile (`Principal + Interest + ExtensionCharges −
      TotalPaid` vs displayed `Balance`, every test loan)
- [ ] Loan statuses correct (expected vs displayed, per known dates/payments
      from Phases 4–9)
- [ ] Loan classifications correct (Phase 9)
- [ ] Gross Receivables reconcile (Phase 12)
- [ ] Bad Loan Receivables reconcile (Phase 9 + 12)
- [ ] Collectible Receivables reconcile (Gross − Bad Loan, Phase 12)
- [ ] Cash on Hand reconciles to Cash Ledger (Dashboard vs Transactions page
      vs `Σ SignedAmount`, Phase 11 + 12)
- [ ] Payment totals reconcile (`Σ Payments.AmountPaid` = `Loan.TotalPaid` =
      Payment History footer, Phase 5)
- [ ] Extension totals reconcile (`Σ Extensions.AdditionalChargesAmount` =
      `Loan.TotalExtensionCharges`, Phase 6)
- [ ] Interest-earned totals reconcile (Phase 14)
- [ ] Monthly interest totals reconcile to contract interest (Phase 14's
      full-lifetime sum check)
- [ ] Interest rounding reconciles at maturity — no drift on a
      non-evenly-divisible term (Phase 14)
- [ ] Statement of Account totals reconcile (Phase 13)
- [ ] Dashboard matches underlying reports (Gross Receivables vs Interest
      Earned Report's Principal totals, internally consistent)
- [ ] Excel exports match screen reports (Phase 14 + 17)
- [ ] PDF exports match screen reports (Phase 14 + 17)
- [ ] Audit logs record financial changes (Phase 15 — every edit/
      classification-change/write-off has a matching `LoanAuditLogEntry`)
- [ ] No orphaned financial transactions exist (Phase 15's delete scenarios
      — this is where the two known gaps above are expected to surface)
- [ ] Editing transactions recalculates affected data (Phase 15, edit side)
- [ ] Deleting transactions recalculates affected data (Phase 15, delete
      side — expected to FAIL against the two known gaps until fixed)
- [ ] No duplicate transactions from double submission (Phase 15)
- [ ] Empty database state works without errors (Phase 2)

## Defect report format

One entry per defect, in the final report:

```
### [Severity: Blocker/Major/Minor/Cosmetic] Short title

- **Where**: page/endpoint/phase
- **Repro**: exact steps, with real IDs/values used
- **Expected**: what should have happened (cite the formula/rule)
- **Actual**: what happened
- **Checklist item(s) violated**: from the Final Reconciliation Checklist
```

Close with a summary table: total checklist items, pass count, fail count,
and the two known gaps' current status (still present / fixed).
