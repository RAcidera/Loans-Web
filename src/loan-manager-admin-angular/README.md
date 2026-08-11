# Loan Manager — Admin Dashboard (Angular, Clean Architecture)

Standalone-component Angular 17+ admin dashboard for a fixed-term
individual-lending business (60-day terms, flat 3% interest, manual
extensions, a separate cash ledger for revolving funds), now wired to a
real backend — see `../backend/` for the .NET 8 + EF Core + SQL Server API
this talks to.

## Layers and the dependency rule

```
presentation/   →   application/   →   domain/   ←   infrastructure/
 (Angular UI)      (use cases)      (entities,          (HttpLoanRepository,
                                      2 repository         HttpCashLedgerRepository,
                                      ports)               Mock* still present)
```

An inner layer never imports from an outer one.

- **`domain/`** — entities (`Loan`, `Customer`, `LoanExtension`, `Payment`,
  `CashLedgerEntry`, `CashSummary`) plus two ports: `LoanRepository` and
  `CashLedgerRepository`.
- **`application/`** — one use case per operation (`GetLoans`,
  `GetLoanDetail`, `RecordPayment`, `ExtendLoan`, `GetCustomers`,
  `GetCashSummary`, `GetCashLedger`, `AddCashTransaction`,
  `GetRecentPayments`).
- **`infrastructure/`** — `HttpLoanRepository` and `HttpCashLedgerRepository`
  now implement the two ports against the real API.
  `MockLoanRepository`/`MockCashLedgerRepository` are still present
  unchanged, for offline demos — see "Switching back to mock data" below.
- **`presentation/`** — Angular components. None of them changed when the
  data source switched from mock to HTTP; they only ever depended on the
  abstract ports.
- **`app.config.ts`** — the composition root, now binding both ports to
  the `Http*` implementations.

## Running against the real backend

1. **Start the backend first** — see `../backend/README.md`. By default it
   listens on `http://localhost:5080` and seeds itself with sample data on
   first run.
2. Confirm `src/environments/environment.ts` points at that same URL
   (`apiBaseUrl: 'http://localhost:5080/api'` by default — change it if
   your backend runs somewhere else).
3. In this Angular project:

   ```bash
   ng add @angular/material   # if not already added
   npm install
   ng serve
   ```

4. Open `http://localhost:4200`. The backend's CORS policy already allows
   this origin (see `Program.cs` on the backend).

If the dashboard loads but shows no data, check the backend console for
seeding errors, and check the browser's network tab for CORS or connection
errors before anything else.

## Switching back to mock data

Useful for demos without a database, or frontend-only development. In
`app.config.ts`:

```ts
// Comment out:
import { HttpLoanRepository } from './infrastructure/repositories/http-loan.repository';
import { HttpCashLedgerRepository } from './infrastructure/repositories/http-cash-ledger.repository';
// Uncomment:
import { MockLoanRepository } from './infrastructure/repositories/mock-loan.repository';
import { MockCashLedgerRepository } from './infrastructure/repositories/mock-cash-ledger.repository';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    // provideHttpClient() no longer needed if going fully mock
    { provide: LoanRepository, useClass: MockLoanRepository }, // was HttpLoanRepository
    { provide: CashLedgerRepository, useClass: MockCashLedgerRepository }, // was HttpCashLedgerRepository
  ],
};
```

No component or use case needs to change either way — that's the point of
depending on the abstract ports rather than a concrete repository.

## A mismatch worth knowing about

The `LoanRepository` port's `extendLoan()` method returns `Observable<Loan>`
(the updated loan), matching what `MockLoanRepository` always returned. But
the backend's `POST /loans/{id}/extensions` endpoint returns the created
`LoanExtensionDto`, not the loan — a POST returning the resource it created
is the more conventional REST shape. `HttpLoanRepository.extendLoan()`
reconciles this by chaining a `GET /loans/{id}` after the POST succeeds
(via RxJS `switchMap`), so the port's contract holds without changing the
backend's endpoint shape. See the comment on that method for the reasoning.

## Design notes

- **Palette**: deep pine (`--lm-primary`, #16423C) for trust/finance, warm
  amber (`--lm-amber`, #E1AA36) for attention (extension points, due-date
  markers), muted rose (`--lm-rose`) for overdue.
- **Type**: Space Grotesk for headings, Inter for UI text, IBM Plex Mono for
  every money figure, date, and ID.
- **Signature visual**: the loan timeline in the loan details dialog —
  start date → due date, with an amber tick at each extension point and a
  "today" marker, turning red once overdue.

## What's built vs. not yet built

| SRS wireframe | Status |
|---|---|
| 2. Dashboard | Built — KPI cards, recent payments feed, loans table |
| 5. Loan Details Page | Built — summary, timeline, payment/extension history, Add Payment / Extend Loan |
| 0. Cash / Funds Page | Built — cash on hand, revolving funds, ledger history, Add Cash Transaction |
| 1. Login | Not built |
| 3. Customers Page (list) | Not built — nav item present, unrouted |
| 4. Customer Profile | Not built |
| 6. Reports Page | Not built |

`preview.html` (in this folder) is a static, dependency-free HTML/CSS/JS
mirror of the design for quick visual reference — not part of the Angular
app, and not included when this project is zipped for delivery.
