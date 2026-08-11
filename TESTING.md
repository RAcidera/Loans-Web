# Requirements Traceability & Test Coverage

This document maps every requirement in the uploaded SRS to what implements
it and — critically — **how confident you should be that it actually
works**, in three tiers:

| Confidence tier | What it means |
|---|---|
| ✅ **Executed & passed** | Actually run, in this environment, against real compiled code. Currently: only the Angular frontend build. |
| 🟡 **Written, not executed** | Code and/or automated tests exist and were reviewed by hand (namespace resolution, signature matching, brace balance, cross-referencing every property name against its declaration), but never compiled or run — no .NET SDK or MySQL server was available in the environment this was built in. |
| ⬜ **Not built** | No implementation exists yet. |

**Action item before you trust any 🟡 row**: run `dotnet build` and
`dotnet test` from `backend/`. That will either confirm everything below,
or surface exactly what to fix — either outcome is more trustworthy than
this document by itself.

---

## 1. Functional Requirements (SRS §3)

| SRS requirement | Implementation | Automated test | Status |
|---|---|---|---|
| 3.1 Add/edit/view customer profiles | `CreateCustomerCommand`, `GetCustomersQuery`, `GetCustomerByIdQuery` | — | 🟡 No handler tests written for Customers yet (lower risk — thin CRUD, no business logic) |
| 3.1 View loan history per customer | `GetLoansByCustomerQuery` | — | 🟡 |
| 3.2 Create loan (principal, start/due date, rate) | `Loan.Originate()`, `CreateLoanCommand` | `LoanTests.Originate_*` (4 tests) | 🟡 |
| 3.2 Default 60-day term, default 3% rate | `Loan.Originate()` defaults; `InterestRate.Default` | `LoanTests.Originate_DefaultTerm_Is60Days`, `InterestRateTests.Default_Is3Percent` | 🟡 |
| 3.2 Auto-calculate interest and total payable | `InterestRate.CalculateInterest()` | `LoanTests.Originate_CalculatesInterestAndTotalDue`, `InterestRateTests.CalculateInterest_IsFlatNotCompounding` | 🟡 |
| 3.2 Track loan status (Active/Extended/Paid/Overdue) | `LoanStatus` enum, `Loan.RefreshOverdueStatus()` | `LoanTests.RefreshOverdueStatus_*` (4 tests) | 🟡 |
| 3.3 Loan extensions with additional fee | `Loan.Extend()`, `ExtendLoanCommand` | `LoanTests.Extend_*` (3 tests), `ExtendLoanCommandHandlerTests` (2 tests), `FunctionalFlowTests.ExtendingALoan_DoesNotCreateAnyCashLedgerEntry` | 🟡 |
| 3.3 Extension history maintained | `Loan.Extensions` collection | `LoanTests.Extend_PushesOutDueDate_AddsFee_MarksExtended` (asserts `Extensions.Count`) | 🟡 |
| 3.3 Recalculate balance after extension | `Loan.Extend()` | `LoanTests.Extend_PushesOutDueDate_AddsFee_MarksExtended` | 🟡 |
| 3.4 Record payments (date, amount) | `Loan.RecordPayment()`, `RecordPaymentCommand` | `LoanTests.RecordPayment_*` (6 tests), `RecordPaymentCommandHandlerTests` (3 tests) | 🟡 |
| 3.4 Support partial and multiple payments | `Loan.RecordPayment()` (additive `TotalPaid`) | `LoanTests.RecordPayment_PartialPayment_*`, `RecordPayment_MultiplePartials_SumCorrectly` | 🟡 |
| 3.4 Auto-update balance | `Loan.Balance` recomputed on every payment/extension | Same as above | 🟡 |
| 3.4 Payment history tracked | `Loan.Payments` collection, `GetLoanDetailQuery` | `LoanTests.RecordPayment_MultiplePartials_SumCorrectly` (asserts `Payments.Count`) | 🟡 |
| 3.4 "Each payment auto-creates a payment_received ledger entry" | `PaymentRecordedDomainEvent` → `PaymentRecordedEventHandler` | `FunctionalFlowTests.RecordingAPayment_AutomaticallyCreatesAMatchingCashLedgerEntry` | 🟡 — **this is the one test I'd most want you to actually run**; it's the whole reason the domain-event architecture exists |
| 3.5 Outstanding loans / overdue loans view | `GetLoansQuery` (computes `Overdue` on read) | `LoanTests.RefreshOverdueStatus_*` | 🟡 |
| 3.5 Total interest earned | *Not separately exposed* — `TotalInterest` exists per-loan but no aggregate "total interest earned across all loans" query exists yet | — | ⬜ Gap — add a query summing `Loan.TotalInterest` across paid/active loans if needed |
| 3.5 Cash on hand / revolving funds (real time) | `GetCashSummaryQuery` — SRS Formulas 1-5 | `CashLedgerEntryTests.IsCashIn_*` (5 cases), `FunctionalFlowTests.RecordingAPayment_*`, `OriginatingALoan_*` | 🟡 |
| 3.5 Summaries per customer/period | *Not built* | — | ⬜ Gap — no reports endpoint exists |
| Cash Ledger: Formula 1 (Total Cash In) | `GetCashSummaryQueryHandler` | `CashLedgerEntryTests.SignedAmount_CashIn_IsPositive` | 🟡 |
| Cash Ledger: Formula 2 (Total Cash Out) | `GetCashSummaryQueryHandler` | `CashLedgerEntryTests.SignedAmount_CashOut_IsNegative` | 🟡 |
| Cash Ledger: Formula 3 (Cash on Hand) | `GetCashSummaryQueryHandler` | `FunctionalFlowTests.RecordingAPayment_*` (asserts the actual before/after delta) | 🟡 |
| Cash Ledger: Formula 4 (Outstanding Principal) | `GetCashSummaryQueryHandler` (sums `Loan.Balance` for active/extended/overdue) | — | 🟡 No dedicated test — covered indirectly, not directly asserted |
| Cash Ledger: Formula 5 (Revolving Funds = Cash on Hand) | `GetCashSummaryQueryHandler` (`RevolvingFunds: cashOnHand`) | — | 🟡 Trivial by construction (same value), not separately tested |

## 2. Non-Functional Requirements (SRS §4)

| SRS requirement | Implementation | Automated test | Status |
|---|---|---|---|
| **Security: user authentication** | `User` aggregate, PBKDF2 hashing, JWT issuance/validation, `AuthController` | `LoginCommandHandlerTests` (4 tests), `AuthenticationTests.Login_*` (4 tests) | 🟡 |
| **Security: basic access control** | `[Authorize]` (all controllers), `[Authorize(Roles = "Admin")]` (loan creation/extension, customer creation, cash transactions) | `AuthenticationTests.ExtendLoan_AsStaff_Returns403`, `AddCashTransaction_AsStaff_Returns403`, `AddCashTransaction_AsAdmin_Returns200`, `RecordPayment_AsStaff_Returns200`, `GetLoans_WithoutToken_Returns401`, `GetLoans_WithValidToken_Returns200` | 🟡 |
| Security: username enumeration resistance | `LoginCommandHandler` (identical exception for both failure modes) | `LoginCommandHandlerTests.Handle_UnknownUsername_*`, `AuthenticationTests.Login_WithUnknownUsername_Returns401_NotSomeOtherStatus` | 🟡 |
| Usability: mobile-friendly interface | Angular responsive layout (grid breakpoints in dashboard/cash-funds SCSS) | — | ✅ Visually reviewed; not tested against real devices/viewports |
| Performance: hundreds of loans without slowdown | No load/perf testing performed | — | ⬜ Not tested at any scale |
| Reliability: secure storage with backups | MySQL + standard backup practices (external to this codebase) | — | ⬜ Operational concern, not code — see README's "Known gaps" |

## 3. What's genuinely verified vs. reviewed-only

**✅ Executed for real:**
- Angular frontend: `ng build` succeeded with zero errors/warnings after two real bugs were found and fixed (a `matCellDef` strict-template typing issue, and Angular 17's `@`-escaping requirement). See `frontend/README.md`.

**🟡 Everything else** — the entire .NET backend, its 27 automated tests
across three test projects, and the SRS's cash-formula logic — has been
written and manually cross-checked (every property name used in EF
configurations and mapping code was grepped against actual entity/DTO
declarations; every test constructor call was checked against actual
handler constructor signatures; namespace resolution and brace balance
were verified programmatically) but **never compiled or run**, because
this environment has neither the .NET SDK nor a MySQL server.

## 4. How to actually verify this yourself

```bash
cd backend
dotnet build                    # will surface any compile errors immediately
dotnet test                     # runs all 27 tests: Domain, Application, and API integration tests
```

`LoanManagementSystem.Api.Tests` needs no MySQL server — it swaps in a
disposable Sqlite in-memory database (see `TestApiFactory.cs`), so
`dotnet test` should work immediately after `dotnet build` succeeds, with
no database setup at all.

If `dotnet test` passes, that's real evidence — not just my say-so — that:
- The domain events correctly wire `Loan` and `CashLedgerEntry` together (`FunctionalFlowTests`)
- JWT auth and role-based authorization actually reject/accept what they should (`AuthenticationTests`)
- The core lending math (interest, partial payments, extensions, overdue detection) is correct (`LoanTests`)

If it doesn't pass, you'll have exact file/line failures to fix — a much
better starting point than the hand-review this document represents.

## 5. Known functional gaps (not a testing problem — nothing was built)

- No Reports page/endpoint (SRS 3.5's "summaries per customer and per period")
- No aggregate "total interest earned" query
- No Login/Customers-list/Customer-profile pages on the frontend (backend endpoints exist for Customers; frontend UI doesn't yet)
- No password reset / user management UI (users are only seeded, not manageable via API or UI)
