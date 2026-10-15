# Implementation Plan — Monthly Capital Growth Dashboard Panel

Source: `assets/Monthly_Capital_Growth_Claude_Code_Requirements.md` (cited
below as **MCG §n**), with `assets/Modern Finance Dashboard UI Mockup.png`
as the layout reference only.

**Current state.** The dashboard (`presentation/dashboard/`) already renders
the seven KPI cards, Collections Overview / Past 7 Days / Receivables
Breakdown charts, and Recent Loans / Recent Payments. Every chart is
hand-rolled inline SVG; **there is no charting library in `package.json`**,
and the component doc comment records that as a deliberate choice. The
backend computes Total Business Position as `Cash on Hand + Gross Receivables`
using only *current* state (`FinancialCalculations.ComputeReceivables` sums
`Loan.Balance`; `ComputeCashOnHand` sums `CashLedgerEntry.SignedAmount`).
Nothing reconstructs Gross Receivables historically. The KPI "vs last month"
badges use a cohort approximation (see `GetDashboardSummaryQueryHandler.PercentChange`),
not true point-in-time values. This plan adds that point-in-time capability as
a new, read-only calculation. It does not touch any existing computation.

---

## Discovery findings (MCG §10.1 — "inspect first")

### Schema and sources

| Requirement term | Actual in this codebase | Notes |
|---|---|---|
| `cash_ledger` | `CashLedgerEntry` aggregate: `TransactionDate` (DateOnly, business date), `TransactionType`, `Amount` (Money ≥ 0), `IsCashIn` (stored), `SignedAmount`, `CreatedAtUtc`, `SourcePaymentId`, `ReferenceId` (loan number) | Single source for Cash on Hand. |
| `loan_ledger` | `LoanLedgerEntry` aggregate: `LoanId`, `TransactionDate` (DateOnly), `TransactionType` ∈ {LoanReleased, InterestAdded, Payment, Extension}, `Debit`, `Credit`, `SignedAmount = Debit − Credit` | Can reconstruct receivables. `RunningBalance` is stamped and documented as unreliable for backdated rows, so it must **not** be used. Recompute from `SignedAmount`. |
| Owner Withdrawal / Owner Deposit | **Already explicit enum members**: `CashTransactionType.OwnerWithdrawal` (fixed cash-out) and `CashTransactionType.OwnerDeposit` (fixed cash-in) | MCG §4.3's "if recorded as generic cash-in/out, propose tagging" **does not apply**. No schema or classification change is needed. Seed data's "Initial capital" is an `OwnerDeposit`. |
| Other cash types | `LoanRelease`, `PaymentReceived` (automatic, mirror loans), `Expense`, `Adjustment` (either direction, caller-specified) | Expenses and adjustments flow into Cash on Hand and therefore into growth. They are never treated as owner movements. |
| Write-offs | `Loan.Status = WrittenOff`. There is **no ledger row**. The date is only in `LoanAuditLogEntry` (`Action = WrittenOff`, `OccurredAtUtc` UTC timestamp) | Current Gross Receivables excludes written-off loans. Historically, a loan must stay in receivables until its write-off instant, converted to a business date. |
| Bad loans | `Loan.Classification = BadLoan`, independent of status | Stays in Gross Receivables (matches MCG §3.1). Classification history isn't needed for this metric. |
| Financial Snapshots | `DiaryFinancialSnapshot`: `GrossReceivables`, `CashOnHand`, `SnapshotDateTimeUtc` (+ collections and releases) | Usable for reconciliation (MCG §8). Not required per month. |
| Business time zone | `IAppDateTimeService` (`Today`, `BusinessTimeZoneId`, `GetStartOfBusinessDayUtc`) and the pure `BusinessTimeZoneCalculator` | Already the house pattern. Reuse it and add no new TZ logic. |
| Dashboard API | `DashboardController` at `api/dashboard`, class-level `[Authorize]` | A new `GET api/dashboard/monthly-capital-growth` fits the convention exactly. |
| Cash ledger drill-down | `/cash-funds` page has `transactionType`, `dateFrom`, `dateTo` filters, but **does not read query params** | Drill-down needs a small additive change to seed filters from the URL. |

### Double-count analysis (MCG §4.1, §9 case 7)

A loan payment produces **both** a `PaymentReceived` cash row (+cash) and a
loan-ledger `Payment` credit (−receivable). A loan release produces a
`LoanRelease` cash row (−cash) and `LoanReleased` + `InterestAdded` debits
(+receivable). Each ledger feeds exactly one metric (cash → Cash on Hand,
loan ledger → Gross Receivables), so mirrored movements net correctly in
Business Position. **No movement is counted twice** as long as each ledger is
only ever summed into its own metric. That rule is an explicit invariant in
the calculator.

### Historical-fidelity gaps (MCG §4.2, §8). Surface these, don't hide them

1. **Edits and deletes are in place, not reversing entries.** `CashLedgerEntry.Revise`/`EditManual`,
   `LoanLedgerEntry.ReviseForPaymentEdit`/`ReviseDebit`/`ReviseExtension`, and the
   `PaymentDeleted`/`LoanExtensionDeleted` handlers (which `Remove` rows) overwrite
   history. Reconstructed months are therefore **"restated as of today's records"**,
   not audit-exact. MCG §4.2 asks us not to present them as audit-exact. Every
   response carries `historyBasis: "restated"`, and the UI says so in a tooltip.
2. **Overpayment clamping.** `Money.Subtract` clamps `Loan.Balance` at 0, while
   `Σ SignedAmount` on the ledger can go negative. Clamp per loan at each cutoff
   (`max(0, Σ SignedAmount)`) so the current month reconciles with the existing
   Gross Receivables KPI.
3. **Pre-ledger loans.** `DbSeeder.BackfillLoanLedgerAsync` populated ledgers for
   older loans from their *current* state, so their intermediate corrections are lost.
   This is the same "restated" caveat as item 1.
4. **Write-off date precision.** The date comes from a UTC audit timestamp converted
   to a business date. A loan with `Status = WrittenOff` but no `WrittenOff` audit
   row is a data-quality warning. Fallback: treat it as written off from its last
   ledger date and flag it.
5. **Start of reliable history.** There's no opening-balance record type. History
   begins at the earliest `CashLedgerEntry.TransactionDate` / `LoanLedgerEntry.TransactionDate`.
   Months before that are `status: "noHistory"` with null amounts, **never zero**
   (MCG §4.2). Production data likely starts mid-2026, so the default 12-month
   view **will** show leading no-history months. The UI must handle this gracefully.
6. **Ledger ↔ aggregate drift.** For each loan, today's `max(0, Σ SignedAmount)`
   should equal `Loan.Balance`. Any mismatch is reported as a warning listing loan
   numbers only (no PII, MCG §6).

### Decisions needed before Phase 1 (MCG §10.2)

No schema change or migration is required, so the risky-migration gate in
MCG §10.2 doesn't trigger. These calls still need sign-off. Defaults are in bold:

| # | Question | Recommended default |
|---|---|---|
| D1 | Written-off loans in historical months | **Include in receivables until the business date of the write-off audit row, exclude after.** This matches the current KPI at "now". |
| D2 | Range options | **6 / 12 / 24 months, default 12.** Use a `mat-select` like other pages. |
| D3 | "vs previous 12 months" % on Capital Growth card (mockup) | **Show only when every month of the prior window has history.** Otherwise hide it, per MCG §2.3. |
| D4 | Mockup label "Owner Contributions" vs requirement "Owner Deposits" | **"Owner Deposits"**, the app's existing term (`CashTransactionType.OwnerDeposit`). |
| D5 | Access | **Same as the rest of the dashboard: any authenticated user.** Staff already see Total Business Position. |

---

## Guiding principles

1. **Additive and read-only.** No writes, no migrations, no change to
   `FinancialCalculations`' existing methods or to any current dashboard number
   (MCG §6, §10.3–10.5).
2. **One formula, one place.** Put the historical calculation in a pure,
   I/O-free class next to `FinancialCalculations`, the same pattern that file's
   doc comment already sets. Handlers fetch the data and the calculator does the math.
3. **Unavailable beats invented.** Any month or amount that can't be trusted is
   returned as null with a reason code, never as 0 (MCG §2.2, §4.2, §11).
4. **Follow the existing chart approach.** Hand-rolled SVG with `appChartTooltip`,
   no new charting dependency. MCG §7 requires justification for one, and the
   existing code deliberately avoids it.
5. **Test the money math hardest.** The calculator gets exhaustive unit tests.
   The UI gets light component tests only.

---

## Phase 0 — Baseline capture (0.5 day)

**Why first:** MCG §9.11 and §11 require proof that existing KPIs are
unchanged. That proof needs a recorded "before".

| Task | Detail |
|---|---|
| `dotnet build` + `dotnet test` | Record the green baseline and test count. |
| `ng build` | Record the baseline. The budget warning is pre-existing. |
| Capture current values | Record `GET api/dashboard/summary` and `GET api/cash-funds/summary` JSON for the dev DB, saved to the scratchpad and not committed. |
| Confirm D1–D5 | Get user sign-off on the decision table above. |

**Acceptance criteria:** the test suite is green, the baseline JSON is
saved, and the decisions are confirmed.

---

## Phase 1 — Pure capital-growth calculator + unit tests (MCG §3, §4, §5, §8, §9)

**Why here:** this is the highest-risk piece, because every number on the panel
comes from it. It's pure, so it can be fully tested before any endpoint or UI
exists.

### Backend

| Capability | Implementation |
|---|---|
| Month boundaries | `Application/Common/Financial/CapitalGrowthCalculator.cs`. Build business-local months `[start, nextStart)` from `IAppDateTimeService.Today`. The current month's cutoff is today (inclusive). Ledger dates are `DateOnly` business dates, so filters compare `DateOnly` directly with no TZ shift (MCG §5). Write-off `OccurredAtUtc` is converted with `BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal`. |
| Single-pass scan (MCG §6 no N+1) | Sort the cash entries and loan-ledger rows by date once. Walk them across month boundaries, accumulating cash, per-loan receivable balance, and owner deposit/withdrawal sums. This is O(n + months). Opening balance at range start is the sum of everything before it. |
| Per-month output | `MonthKey` (yyyy-MM), `Start`, `EndCutoff`, `IsPartialMonth`, `Begin/EndCash`, `Begin/EndGrossReceivables` (Σ per-loan `max(0, bal)` excluding loans written off on or before cutoff), `Begin/EndPosition`, `OwnerWithdrawals`, `OwnerDeposits`, `PositionChange`, `AdjustedGrowth = change + withdrawals − deposits`, `GrowthPercent` (null unless beginning position > 0, rounded 2 dp), `Status` ∈ {complete, partialMonth, noHistory, partialHistory}. |
| Summary | `TotalGrowth` (Σ over months with history), `AverageMonthlyGrowth` both **completed-months-only** and **including MTD**, `BestMonth` (completed months only), and `PreviousPeriodGrowth` (null when prior window incomplete, per D3). |
| Invariant self-checks (MCG §8) | Per month, assert `End − Begin = Change`, `Change + W − D = Growth`, and `End = EndCash + EndGross`. A failure becomes a `warnings[]` entry, never an exception or a silent correction. |
| Data-quality checks | Ledger ↔ `Loan.Balance` drift (finding 6), written-off loans without an audit row (finding 4), negative cash at any month end, and unknown `CashTransactionType` (future-proofing). Each warning has a code plus loan numbers. No names. |
| Money type | `decimal` throughout. No `double` anywhere in the path. |

### Tests: `tests/LoanManagementSystem.Application.Tests/CapitalGrowthCalculatorTests.cs`

Cover every MCG §9 scenario against hand-computed expectations. Scenario 1 must
use the MCG §3.2 example (₱1,441,481 → ₱1,457,212, ₱30,000 withdrawal ⇒
+₱45,731, 3.17%):

1. Release + collection + interest + extension in one month.
2. Withdrawal only, deposit only, both, and several of each.
3. Position falls but growth is positive because of withdrawals.
4. Deposit-only month gives growth = 0.
5. Zero or negative beginning position gives `GrowthPercent` null.
6. Bad loan stays included, write-off drops out from its date, overpayment clamps,
   and a backdated payment lands in its effective month.
7. Mirrored payment counted once in position.
8. No-activity month (growth 0, status complete) versus pre-history month
   (amounts null, status noHistory).
9. Dec→Jan rollover, Feb 29 2028, partial current month, and a Manila midnight
   boundary for the write-off UTC timestamp.
10. 24-month range, and 10k-row synthetic ledger completing in < 200 ms.

**Acceptance criteria:** all scenarios pass, and the §3.2 example reproduces
exactly. The current-month `EndPosition` with today's cutoff equals
`ComputeReceivables(...).Gross + ComputeCashOnHand(...)` on the same fixture.

**Effort:** 2–3 days.

---

## Phase 2 — Query, endpoint, reconciliation (MCG §6, §8)

**Why here:** it's a thin wrapper over Phase 1. It depends on Phase 1 and nothing
else depends on it except the frontend.

### Backend

| Capability | Implementation |
|---|---|
| Repository reads | New `ILoanLedgerRepository.GetAllAsync` (`AsNoTracking`). New `ILoanAuditLogRepository.GetWriteOffsAsync` (`Action == WrittenOff`, returns `LoanId` + `OccurredAtUtc`). `ICashLedgerRepository.GetAllAsync` already exists. This is three queries total. |
| Query | `Application/Loans/Queries/GetMonthlyCapitalGrowth/GetMonthlyCapitalGrowthQuery.cs` (`int Months`, validated ∈ {6, 12, 24}, else `DomainException` → 400). It loads the ledgers, write-offs, and loans (for the drift check and loan numbers), then calls the calculator. |
| Reconciliation (MCG §8) | In the handler, compare current-month `EndPosition` against the live KPI formula on the same loaded data, and return `reconciliation.currentPositionMatches` + `difference`. Also compare each `DiaryFinancialSnapshot` falling inside the range against the reconstructed position at that snapshot's business date. List differences as informational (expected after backdated edits) and **never adjust** to them. |
| DTO | `Common/DTOs/MonthlyCapitalGrowthDto.cs`: `timeZone`, `asOfUtc`, `asOfDate`, `currency: "PHP"`, `months`, `historyBasis: "restated"`, `historyStartDate`, `series[]`, `summary`, `reconciliation`, `warnings[]`. Status values are lowercase strings, following the wire-format convention. The selected-month breakdown is just a `series[]` element, so it isn't duplicated in the payload. |
| Endpoint | `GET api/dashboard/monthly-capital-growth?months=12` on `DashboardController`. It inherits the class `[Authorize]` (D5) and contains no PII. |
| Index | None up front. The three queries are full-table scans in the same way `GetCashSummaryQuery` already is. Revisit only if Phase 4's timing check fails. |

### Tests
- `Api.Tests` integration (Sqlite): 200 with the expected shape, 401 when
  unauthenticated, 400 for `months=7`.
- Regression test: `api/dashboard/summary` and `api/cash-funds/summary` give
  identical results before and after the feature, on a seeded DB (MCG §9.11).

**Acceptance criteria:** with the API running against the dev DB, Swagger returns
12 months. The current month's `endPosition` equals the dashboard's Total Business
Position to the centavo, and `reconciliation.currentPositionMatches` is true.

**Effort:** 1–1.5 days.

---

## Phase 3 — Angular panel (MCG §2, §7)

**Why here:** it needs the real API contract from Phase 2. It's the largest
phase but low financial risk, because it's pure presentation.

### Frontend

| Component / file | Purpose |
|---|---|
| `domain/entities/capital-growth.entity.ts` | Interfaces mirroring the DTO field for field. |
| `domain/repositories/dashboard…` | Add `getMonthlyCapitalGrowth(months)` to the **existing** port that serves `getDashboardSummary` (same lifecycle boundary: dashboard analytics), and implement it in its `Http*` class. Check `app.config.ts` for the binding. No new provider should be needed. |
| `application/use-cases/get-monthly-capital-growth.use-case.ts` | A thin wrapper, like the other ~10-line use cases. |
| `presentation/dashboard/capital-growth-panel/` (standalone child component) | Keeps `dashboard.component.ts` from growing past ~260 lines. Its own `.scss`, duplicating `.mono`, the card, and the chip styles per the house convention. |
| Chart | Hand-rolled SVG (principle 4) with **two labeled Y-axes**: growth on the left, zero-baseline-aware, bars green/red/neutral via `--lm-success`/`--lm-danger`. Business Position on the right, as a purple line (`--lm-primary`). Use `appChartTooltip` on bars and points. The current month gets a hatched or outlined bar and a "Month to Date" label. No-history months show as an empty, shaded slot labeled "No history", never a zero bar. |
| Interaction and accessibility | Each month is a focusable `<g role="button" tabindex="0" [attr.aria-label]="'Oct 2026: adjusted growth +₱45,731, position ₱1,457,212'">`. Click, Enter, or Space selects the month, and Left/Right arrows move the selection. The default selection is the latest completed month, or the MTD month if that's the only one available. |
| Summary cards | Capital Growth (range), Average Monthly Growth (the main figure is completed months only, with "incl. MTD: ₱x" as a sub-line), and Best Performing Month (completed only). |
| Selected-month breakdown | The six line items plus prominent Adjusted Growth, Growth % or "N/A", and an ⓘ tooltip: *"Change in business position, adding back owner withdrawals and removing owner deposits. Includes uncollected interest and extension charges. Not profit, revenue, or cash flow."* For the current month: "as of {date}". A "View cash transactions →" link goes to drill-down (Phase 4). |
| Range selector | `mat-select` 6 / 12 / 24 (D2). It refetches, and selection resets to the default. |
| States | Loading (skeleton), error (inline `<p>` keyed on `err.status`, mirroring `login.component.ts`), all-no-history ("Not enough history yet. Capital growth appears once a full month of ledger data exists."), `warnings[]` (an inline warning strip listing human-readable messages), and a `historyBasis` note in the footer. |
| Layout | The panel goes between `.kpi-row` and `.charts-row` in `dashboard.component.html`. It uses a grid of `chart | side-cards` on wide screens and stacks below ~1100px, with the chart scrolling horizontally at phone width if needed. |
| Formatting | Reuse the `fmtMoney`/`fmtCompactMoney` behavior. Month labels come from the API's `monthKey` and are never derived from browser `Date` objects, so the browser TZ can't shift them (MCG §5). |

**Acceptance criteria:** the panel appears under the KPI row and matches the mockup
layout. Hovering shows tooltips. Click or keyboard updates the breakdown. Switching
to 24 months refetches. With the API stopped, an inline error shows and the rest of
the dashboard still renders. Existing charts and cards look and behave exactly as
before.

**Effort:** 3–4 days.

---

## Phase 4 — Drill-down, validation and delivery report (MCG §2.4, §8, §10.6–10.7)

**Why last:** it depends on everything above, and it's the sign-off gate.

| Task | Detail |
|---|---|
| Cash ledger deep link | `cash-funds.component.ts` reads optional `dateFrom`/`dateTo`/`transactionType` query params on init and seeds the existing filter form with them. This is additive, and the page with no params behaves exactly as today. The breakdown links to `/cash-funds?dateFrom=2026-10-01&dateTo=2026-10-31`, with optional per-line links for owner withdrawals and deposits. |
| Light component spec | `capital-growth-panel.component.spec.ts`: renders no-history slots (not zero bars), N/A growth %, and keyboard selection. |
| Reconciliation run | Against the dev or prod-copy DB: confirm current-month position = KPI, list snapshot differences, and list any drift warnings. |
| Performance check | Time the endpoint on the largest available dataset (target < 1 s, consistent with the existing plan's Phase 6 bar). |
| Regression | Re-run Phase 0's captures and diff them. They must be identical apart from time-moving fields. Run the full `dotnet test` and `ng build`. |
| Delivery report (MCG §10.7) | Changed-file list, API contract, "no DB/index changes", test results, reconciliation results, and outstanding data-quality limitations (the "restated" basis, write-off date precision, pre-history months). |

**Acceptance criteria:** every MCG §11 bullet is checked off against the running app.

**Effort:** 1 day.

---

## Out of scope (explicitly)

- Collectible Capital Growth (MCG §4.4): deferred until approved.
- Any migration, backfill, reclassification, or change to ledger write paths (MCG §10.4–10.5).
- Making history audit-exact (this would require append-only reversing entries
  across both ledgers, a separate and much larger change).

## Summary timeline

| Phase | Focus | Effort | Blocks later phases? |
|---|---|---|---|
| 0 | Baseline + decision sign-off | 0.5 day | Yes |
| 1 | Pure calculator + exhaustive tests | 2–3 days | Yes, everything uses it |
| 2 | Query, endpoint, reconciliation | 1–1.5 days | Blocks Phase 3's real data |
| 3 | Angular panel | 3–4 days | No |
| 4 | Drill-down, validation, delivery report | 1 day | No (final gate) |

**Total: roughly 7.5–10 working days.**

**Parallelization:** once Phase 2's DTO shape is agreed (it can be fixed at the
start of Phase 1), Phase 3 can start against a hand-written stub response while
Phase 1–2 are built. Phase 4's cash-ledger deep link is independent and can be
done at any time.
