---
name: implement-loans-page
description: Implement the standalone Loans list page (SRS 3.2/3.5 "outstanding loans" view) at the /loans route — currently commented out in app.routes.ts, with no LoansComponent built yet. Use when the user asks to "build the loans page", "implement the loans list", "add the /loans route", or references the still-unbuilt Loans nav item.
---

# Implement the Loans page

The admin shell's nav already has a "Loans" item pointing at `/loans`
(`presentation/admin-shell/admin-shell.component.ts`), and `app.routes.ts` has
it commented out:
```ts
// { path: 'loans', component: LoansComponent },
```
No `LoansComponent`, `create-loan.use-case.ts`, or `add-loan-dialog` exist yet
— this is a from-scratch page, not an edit to something partial. Re-check
that these files are still absent before starting (grep the presentation and
application/use-cases folders) — this skill may be stale if someone's already
built it since it was written.

## The backend is already 100% done — this is frontend-only

`CreateLoanCommand`, `GetLoansQuery`, and `LoansController` (`GET /api/loans`,
`POST /api/loans`, plus the `/{id}/detail`, `/{id}/extensions`,
`/{id}/payments` sub-resources) already exist and are exercised by the
dashboard today. Confirm this is still true
(`src/LoanManagementSystem.Api/Controllers/LoansController.cs`) before
assuming no backend work is needed — if a future change removed or altered
these, flag it rather than silently building against a stale assumption.

`GetLoansUseCase` (`application/use-cases/get-loans.use-case.ts`) already
exists and already powers the dashboard's table — reuse it as-is, don't
duplicate it.

## What doesn't exist yet and needs building

| File | Purpose |
|---|---|
| `application/use-cases/create-loan.use-case.ts` | Thin wrapper over a new `LoanRepository.createLoan(...)` method — mirrors `create-customer.use-case.ts` |
| `presentation/loans/loans.component.ts` (+ `.html`, `.scss`) | The full loans list — every loan, not just the dashboard's paginated subset |
| `presentation/add-loan-dialog/` (+ `.html`, `.scss`) | Form dialog to originate a new loan (SRS 3.2), same shape as `add-customer-dialog` |
| `app.routes.ts` | Uncomment the `/loans` route |

### `LoanRepository` port addition

`domain/repositories/loan.repository.ts` has no `createLoan` method yet.
Add one, mirroring `createCustomer`'s shape:
```ts
abstract createLoan(customerId: string, principal: number, interestRate?: number, termDays?: number, startDate?: string): Observable<Loan>;
```
Implement it in `HttpLoanRepository` (`POST /api/loans` — see
`CreateLoanCommand`'s optional `InterestRate`/`TermDays`/`StartDate` fields
for the exact request shape) and in `MockLoanRepository` (push onto its
in-memory `loans` array, same pattern as its `createCustomer`).

### `loans.component` — mirror `customers.component`, not the dashboard

The closest analog is `presentation/customers/customers.component.ts` (a
plain searchable/sortable table + an Admin-gated "Add" button), **not** the
dashboard — the dashboard's version is a KPI-card-plus-abbreviated-table
composite; this page is the full list. Reuse:
- Column set: `loanNumber` (see the "loan numbering" note below —
  **never** show `loan.loanId`, that's the raw internal GUID), customer
  name, principal, due date, balance, status, actions.
- `MatTableDataSource` + `MatSort` + `MatPaginator` + a search input calling
  `dataSource.filter = value.trim().toLowerCase()`, same as
  `customers.component.ts`.
- Status chip styling/labels — copy the `STATUS_LABEL` map and `.chip--*`
  SCSS classes from `dashboard.component.ts`/`.scss` (`active`, `extended`,
  `paid`, `overdue`).
- "Add loan" button gated by `authService.hasRole('admin')` (the backend's
  `POST /api/loans` is `[Authorize(Roles = "Admin")]`), opening
  `AddLoanDialogComponent` — same `dialog.open(...).afterClosed().subscribe(result => { if (result?.added) this.load(); })`
  pattern as `customers.component.ts`'s `openAddCustomer()`.
- Row click opens `LoanDetailsDialogComponent` — **reuse it, don't rebuild
  loan detail viewing.** Same invocation as `dashboard.component.ts`:
  ```ts
  this.dialog.open(LoanDetailsDialogComponent, { width: '640px', maxWidth: '95vw', data: { loanId: loan.loanId }, autoFocus: false });
  ```
  (Yes, `loanId` — the dialog's `data` still needs the real GUID to fetch
  loan detail by ID; it's only *display* that uses `loanNumber`.)

### The row-click + button-click bug — don't reintroduce it

`dashboard.component.ts`'s loans table (and `customer-profile.component.ts`'s)
both had a bug where the table row's `(click)="openLoanDetails(row)"` AND a
per-row "view" icon button's `(click)="openLoanDetails(loan)"` both fired on
a button click (click events bubble), opening **two stacked dialogs** at
once. Both were fixed by adding `$event.stopPropagation()` to the button's
click handler:
```html
<button mat-icon-button (click)="openLoanDetails(loan); $event.stopPropagation()" aria-label="View loan details">
```
If `loans.component.html` has both a row click and a per-row action button
doing the same thing, apply the same fix from the start — don't reproduce
the bug a third time.

### `add-loan-dialog` — mirror `add-customer-dialog` / `add-payment-dialog`

Fields: customer (a `mat-select` populated from `GetCustomersUseCase`,
already exists), principal amount (required, `Validators.min(1)`), and
optionally interest rate / term days / start date if the UI should expose
overriding the SRS defaults (3%, 60 days) — otherwise omit them from the
form and let the backend apply its own defaults by not sending those fields.
Same `submitting` flag + `dialogRef.close({ added: true })` pattern as the
other two dialogs. SCSS is just `@use '../_shared-dialog-form' as *;`.

### Loan numbering — already solved, just consume it

`Loan.LoanNumber` (backed by a real MySQL `AUTO_INCREMENT` column, see
`LoanConfiguration.cs` / `AppDbContext.OnModelCreating`) and `LoanDto.LoanNumber`
(the formatted `"LM-001"` string, via `MappingExtensions.FormatLoanNumber`)
already exist and are returned by every loan-related endpoint. The Angular
`Loan` entity already has a `loanNumber: string` field. Display *that*
everywhere a loan needs to be shown to a human — never `loan.loanId` (the
raw GUID), which exists only for API routing/dialog `data` params.

## Verification

- `ng build` clean (no backend changes needed unless the `LoansController`
  assumption above turned out to be wrong).
- No browser automation is available in a typical headless run of this
  skill — verify the data path via direct HTTP calls instead: log in, `POST
  /api/loans` with a test customer id, confirm it appears in `GET
  /api/loans`, confirm the response's `loanNumber` looks like `"LM-0NN"` and
  is unique. Flag to the user that an actual browser click-through (Add
  Loan dialog opens, table renders, row click opens the loan-details
  dialog exactly once) still needs manual confirmation — this project's own
  standing rule is that UI changes need a real browser check before being
  called done.
