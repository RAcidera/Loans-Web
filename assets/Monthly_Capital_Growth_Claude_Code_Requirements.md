# Claude Code Requirements — Monthly Capital Growth Dashboard

## 1. Objective
Implement the **Monthly Capital Growth** dashboard section based on the approved visual mockup found in assets/Modern Finance Dashboard UI Mockup.md. Preserve the existing lending dashboard and add a full-width capital-growth analytics panel **between the top KPI cards and the existing Collections Overview / Past 7 Days / Receivables Breakdown charts**.

**Application:** Existing production ASP.NET Core API + Angular frontend + Microsoft SQL Server database.

**Important:** First inspect the existing codebase, database schema, Cash Ledger, loan ledger, settings, reporting conventions, and dashboard components. Adapt the specification to actual names and existing business rules. Do not create duplicate accounting sources or overwrite historical financial data.

## 2. UI Requirements

### 2.1 Placement and styling
- Retain the current seven KPI cards: Gross Receivables, Collectible Receivables, Bad Loan Receivables, Cash on Hand, Total Business Position, Active Loans, Overdue Loans.
- Add a responsive, full-width **Monthly Capital Growth** card immediately below these KPIs.
- Retain the existing charts and Recent Loans / Recent Payments sections below it.
- Match the current dashboard's compact card design, typography, white surfaces, subtle borders, purple accents, green positive indicators, and red negative indicators.
- Follow the provided mockup as a **layout reference**, not as a source of production numbers. All amounts must come from actual application data.

### 2.2 Main chart
- Heading: **Monthly Capital Growth**.
- Subtitle: **Business position and adjusted capital growth**.
- Default range: **Last 12 Months**, including the current partial month. Provide range selection if consistent with existing dashboard patterns (e.g., last 6 / 12 / 24 months).
- X-axis: chronological month and year.
- **Bars:** monthly Adjusted Capital Growth in PHP; positive bars green and negative bars red; zero values neutral.
- **Line:** end-of-month Total Business Position in purple.
- Use **separate labeled Y-axes** (growth and position) or two synchronized charts, because their scales can differ greatly. Clear legend and hover tooltips are required.
- Current month must be clearly marked **Month to Date**; earlier months are completed months.
- Clicking a bar or line point selects that month and updates the detail panel. Keyboard interaction and accessible labels are required.
- Handle loading, empty, insufficient-history, negative-value, and error states without fake data.

### 2.3 Right-side summary cards
Show three compact cards, derived from the selected period:
1. **Capital Growth (Last 12 Months)** — sum of the 12 monthly adjusted growth values; do not show a comparison percentage unless a valid previous comparison period exists.
2. **Average Monthly Growth** — average across the displayed months; explicitly distinguish full-month average from averages including the current partial month.
3. **Best Performing Month** — month with highest adjusted capital growth, and its amount. For a fair completed-month ranking, exclude the current partial month unless clearly labeled otherwise.

### 2.4 Selected-month breakdown
When a month is selected, show:
- Beginning Business Position
- Ending Business Position
- Change in Business Position
- Owner Withdrawals
- Owner Deposits
- **Adjusted Capital Growth** (prominent)
- Growth % of beginning business position, only if beginning position is positive and nonzero; otherwise show N/A.
- For the current month, ending position is the **current as-of time**, not a future month-end estimate.
- Allow drill-down to relevant Cash Ledger transactions where existing routes permit.
- All cards, tooltips, month labels, and currency values must respond to the selected month/range.

## 3. Financial Definitions

### 3.1 Business Position
Use the application's existing definition:

`Total Business Position = Cash on Hand + Gross Receivables`

`Gross Receivables = Outstanding Principal + Outstanding Interest + Outstanding Extension Charges - Applicable Payments` (use the application's existing outstanding-balance computation; **do not subtract payments twice**).

Bad-loan balances remain included in Gross Receivables unless the existing application has explicitly written them off. Preserve current write-off and adjustment handling.

### 3.2 Adjusted Capital Growth
For each month:

```
ChangeInBusinessPosition = EndingBusinessPosition - BeginningBusinessPosition
AdjustedCapitalGrowth = ChangeInBusinessPosition
                      + OwnerWithdrawals
                      - OwnerDeposits
GrowthPercent = AdjustedCapitalGrowth / BeginningBusinessPosition * 100
```

Use **net positive owner withdrawals** and **net positive owner deposits** during the month, accounting for transaction reversals/voids under existing rules. Avoid double-counting transfers.

**Example (illustrative only):**

```
Beginning Business Position   ₱1,441,481
Ending Business Position      ₱1,457,212
Position Change                 +₱15,731
Owner Withdrawals               ₱30,000
Owner Deposits                  ₱0
Adjusted Capital Growth         +₱45,731
Growth %                           3.17%
```

**Labeling:** This is **adjusted capital-position growth**, **not realized net profit or revenue**. Gross receivables may include uncollected interest and extension charges; do not label this metric as earnings, profit, ROI, or cash flow. Add a brief tooltip explaining this distinction.

## 4. Data Sources and Historical Reconstruction

### 4.1 Inspect existing sources first
- `cash_ledger`: cash inflows/outflows, loan releases, payments, owner withdrawals, owner deposits, expenses, corrections, opening balance, and related transaction types.
- `loan_ledger`: dated debit/credit loan movements (principal, interest, extension charges, payments, adjustments) and loan identifiers.
- Existing loan status/classification/write-off tables and services.
- Existing Financial Snapshot/Diary records, including captured totals and timestamps.

**Do not assume `cash_ledger` alone can reconstruct receivables.** Cash and receivables must be calculated from their appropriate ledgers. Inspect whether loan payments or releases are mirrored in both ledgers, and count each movement only in its appropriate metric.

### 4.2 Historical as-of calculations
For each month use business-local boundaries and a consistent **as-of cutoff**:
- Beginning position: position immediately before the first instant of the month.
- Ending position: position immediately before the first instant of the next month (or current as-of instant for current month).
- Cash as-of cutoff: opening cash balance plus eligible cash movements through the cutoff.
- Gross receivables as-of cutoff: opening loan balances plus dated loan-ledger debits minus credits through the cutoff, applying existing void/write-off/adjustment semantics.
- Only include records with effective transaction dates at or before the cutoff; distinguish `date` business fields from UTC audit timestamps.
- Validate historical behavior of edited, deleted, backdated, and reversed transactions. Do not present an audit-exact historical value if prior versions cannot be reconstructed.
- If the historical opening balance or transaction history is incomplete, return **unavailable/partial** with a clear reason. Do not substitute zero or fabricate months.
- Existing manual Diary snapshots can be used for reconciliation/reference but should not be required for every month.

### 4.3 Owner movements
- Locate existing Cash Ledger transaction types for **Owner Withdrawal** and **Owner Deposits**.
- If these are currently recorded as generic cash-in/cash-out, **do not guess** from remarks. Identify the need for classification and propose a safe, backward-compatible method for tagging them.
- Exclude ordinary loan collections, loan releases, bank-to-cash transfers, and operating expenses from owner movements.
- Ensure owner movements are included in Cash on Hand but added back/subtracted exactly once in Adjusted Capital Growth.

### 4.4 Conservative metric (future enhancement)
Potential later option: `Cash on Hand + Collectible Receivables` for a **Collectible Capital Growth** comparison. **Do not implement in this phase** unless explicitly approved.

## 5. Date and Time Rules
- Respect the configured **Business Time Zone**, default `Asia/Manila` where appropriate; never depend on server or browser timezone.
- Use business-local calendar months and half-open intervals `[start, nextStart)`.
- UTC timestamp filtering must use boundaries converted from business-local time.
- Business-date-only fields must not shift due to timezone conversion.
- Month labels, current-month cutoff, and tooltips must all use the same timezone.

## 6. Backend and API
- Reuse existing dashboard/report services, dependency injection, authentication, authorization, and API conventions.
- Add a dedicated read-only analytics endpoint, for example `GET /api/dashboard/monthly-capital-growth?months=12`, **only if it fits current routing conventions**.
- Response should include: timezone, as-of timestamp, range, currency, monthly series, period summary, selected-month breakdown, and completeness/quality indicators.
- Per month include: month key, start/end cutoff, isPartialMonth, beginning/ending cash, beginning/ending gross receivables, beginning/ending business position, owner withdrawals, owner deposits, change, adjusted growth, growth percent (nullable), and data completeness.
- Use SQL `decimal` / .NET `decimal` for financial amounts. Never use floating-point calculations for money.
- Avoid N+1 monthly queries. Prefer efficient grouped queries or a single ledger scan per relevant date range, with opening balances computed once.
- Use appropriate indexes only after reviewing existing schema and query plans.
- Protect endpoints using existing dashboard permissions. Do not expose borrower PII in chart payloads.
- Do not add write operations or modify existing balances to generate analytics.

## 7. Angular Implementation
- Integrate with existing dashboard architecture and chart library; do not introduce a second charting dependency without justification.
- Use responsive layout: wide screens show chart on left and summary/detail cards on right; smaller screens stack them.
- Provide accessible hover/focus tooltips, currency formatting, selected-month state, and clear positive/negative visual treatment.
- Handle data loading, API failure, missing owner classifications, missing opening balances, and partial-month data.
- Preserve existing dashboard cards, chart toggles, routes, and list functionality.

## 8. Reconciliation and Data Quality
For each computed month, validate:

```
EndingPosition - BeginningPosition = PositionChange
PositionChange + Withdrawals - Deposits = AdjustedGrowth
EndingPosition = EndingCash + EndingGrossReceivables
```

- Reconcile the current-month ending position against the existing Dashboard **Total Business Position** at the same as-of cutoff.
- Reconcile historical month-end results with any available trustworthy Financial Snapshots, documenting expected differences from later backdated corrections.
- Check for missing ledger history, unknown transaction types, reversals, write-offs, duplicate entries, and negative cash.
- Surface a clear warning if historical numbers cannot be trusted. Do not silently force reconciliation or change financial records.

## 9. Test Scenarios
Implement unit/integration tests covering:
1. Simple month with loan releases, collections, interest, and extensions.
2. Owner withdrawal only; deposit only; both; multiple transactions.
3. Business-position decline but positive adjusted growth due to withdrawals.
4. Positive position change caused only by owner deposit (adjusted growth should be zero).
5. Zero and negative beginning position (growth % unavailable).
6. Bad loans, write-offs, loan reversals, refunds, and edited/backdated transactions.
7. Cash/loan ledger entries referring to the same payment (no double count).
8. Months with no activity and months before reliable history begins.
9. Year rollover, leap years, partial current month, and timezone boundaries.
10. More than 12 months, missing/unknown owner classifications, and pagination/performance with production-scale records.
11. Regression tests confirming existing dashboard KPI values and other charts remain unchanged.

## 10. Production Safety and Delivery Process
1. **Inspect first.** Report the relevant schema, current calculation logic, source transaction types, and any gaps in historical completeness.
2. Present a concise implementation plan and any required schema/classification changes **before making risky migrations**.
3. Implement additive, backward-compatible backend and Angular changes.
4. Do not rewrite `cash_ledger`, `loan_ledger`, Financial Snapshots, or production financial data.
5. Avoid automatic historical backfills that assume ambiguous transactions are owner movements.
6. Run unit, integration, and UI regression tests; validate current values against existing dashboard.
7. Provide changed-file list, API contract, database/index changes (if any), test results, reconciliation results, and outstanding data-quality limitations.

## 11. Acceptance Criteria
- A compact Monthly Capital Growth panel appears under the existing KPI row and matches the approved mockup's layout.
- The chart shows 12 months of **actual** growth bars and business-position line, with clearly separated scales.
- The selected-month panel explains exactly how adjusted growth is calculated.
- Owner withdrawals/deposits come from verified ledger classifications.
- All calculations respect business-local month boundaries.
- Current ending position reconciles with existing Total Business Position.
- Incomplete historical data is explicitly labeled, not invented.
- Existing dashboard and production financial behavior remain unchanged.

**Implementation note:** The supplied image is a visual reference only. Its sample monthly values, period labels, and growth totals are not authoritative accounting data.
