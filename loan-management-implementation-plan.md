# Implementation Plan — Loan Management System
## Remaining Work, Sequenced

This plan picks up from the current state: Clean Architecture + DDD .NET
backend (Domain/Application/Infrastructure/Api), Angular frontend with the
same layering, JWT auth, and a 27-test suite — all **written but never
executed** (see `TESTING.md` in the backend delivery). Everything below
assumes that architecture and extends it rather than replacing it.

---

## Guiding principles for every phase

1. **Verify before building.** Phase 0 isn't optional — building on top of
   unverified code compounds risk.
2. **Follow the existing pattern, don't invent a new one.** Every backend
   feature = domain change (if any) → use case(s) → EF config (if new
   entity) → controller endpoint. Every frontend feature = domain entity →
   use case → component, wired through the existing repository ports.
3. **Test the thing that's actually risky.** Domain logic gets unit tests;
   cross-cutting concerns (auth, money movement) get integration tests;
   pure UI doesn't need either.
4. **One phase should be shippable before starting the next** — each phase
   below ends in a working, demoable increment, not a partial state.

---

## Phase 0 — Verify the existing build (prerequisite for everything else)

**Why first:** every phase below adds code on top of a backend that has
never actually compiled. Finding a foundational bug after Phase 3 is built
on top of it costs more than finding it now.

| Task | Detail |
|---|---|
| Run `dotnet build` | Fix any compile errors — expected candidates: EF Core value-converter signatures, MediatR version mismatches, namespace typos in the newer auth files |
| Run `dotnet ef migrations add InitialCreate` | First real test of every `IEntityTypeConfiguration<T>` |
| Run `dotnet ef database update` against a real MySQL instance | Confirms the schema actually creates |
| Run `dotnet test` | All 27 tests — Domain, Application, API integration (Sqlite-backed, no MySQL needed) |
| Run `ng serve` against the running API | Manually click through: login, dashboard loads, loan details dialog opens, record a payment, confirm cash-funds page reflects it |

**Acceptance criteria:** `dotnet test` green, manual click-through of the
five built pages works with real network calls, no console errors.

**Effort:** 0.5–1 day, assuming no major surprises. Budget 2–3 days if the
EF value-converter or migration step surfaces real issues (this is the
highest-risk unverified area — see backend README's "Known gaps").

---

## Phase 1 — Customers Page + Customer Profile (SRS wireframes 3–4)

**Why second:** the backend for this is already 90% done
(`GetCustomersQuery`, `CreateCustomerCommand`, `GetLoansByCustomerQuery`,
`CustomersController`) — this phase is almost entirely frontend, making it
the fastest way to close a visible gap and validate the existing backend
endpoints get real traffic.

### Backend (small additions only)
- `UpdateCustomerCommand` + handler (edit profile — SRS 3.1 "edit... customer profiles" isn't covered yet, only add/view)
- `PUT /api/customers/{id}` on `CustomersController`

### Frontend
| Component | Purpose |
|---|---|
| `application/use-cases/get-customers.use-case.ts`, `create-customer.use-case.ts`, `update-customer.use-case.ts` | Thin wrappers, same pattern as existing loan use cases |
| `presentation/customers/customers.component.ts` | List page — table (name, contact, borrower type, status, loan count), search bar, "Add Customer" button |
| `presentation/customer-profile/customer-profile.component.ts` | Detail view — profile card + editable form, loans table scoped to this customer (reuse the existing loans table styling/columns from the dashboard) |
| `presentation/add-customer-dialog/` | Form dialog, same shape as `add-payment-dialog` |
| Route additions | `/customers`, `/customers/:id` in `app.routes.ts`, guarded like the rest |

**Acceptance criteria:** Add a customer, see it in the list, click through
to their profile, see their loans, click a loan to open the existing loan
details dialog (reuse, don't rebuild).

**Effort:** 2–3 days.

---

## Phase 2 — Reports Page (SRS wireframe 6, SRS 3.5)

**Why third:** genuinely new backend logic (aggregation queries that don't
exist yet), so it benefits from Phase 0's verified foundation and Phase 1's
proof that the add-a-page pattern works end-to-end.

### Backend
| New capability | Implementation |
|---|---|
| Total interest earned (SRS 3.5, currently a documented gap) | `GetInterestSummaryQuery` — sums `Loan.TotalInterest` for Paid/Active/Extended loans, likely split by date range |
| Summaries per customer | `GetCustomerSummaryQuery` — total borrowed, total paid, loans count, per customer |
| Summaries per period | `GetPeriodSummaryQuery(startDate, endDate)` — loans originated, payments collected, extensions granted, interest earned, all filtered to the range |
| Export | Decide CSV vs. PDF now — CSV is a half-day of work (`System.Text` writer, no new dependency); PDF needs a library (e.g. QuestPDF) and is closer to 2 days. Recommend CSV first, PDF as a follow-up if actually requested |
| New endpoints | `GET /api/reports/interest-summary`, `GET /api/reports/customer-summary`, `GET /api/reports/period-summary?start=&end=`, `GET /api/reports/export?format=csv` |

### Frontend
- `presentation/reports/reports.component.ts` — date range filter (reuse Angular Material's date range picker), summary cards (mirroring the dashboard KPI card pattern), a loans/payments table for the selected period, an "Export" button
- New use cases mirroring the three new queries

**Acceptance criteria:** Select a date range, see interest earned / loans
originated / payments collected for that range, export a CSV that opens
correctly in a spreadsheet.

**Effort:** 3–4 days (add 1–2 days if PDF export is required, not CSV).

**New tests needed:** unit tests for each aggregation query's math
(mirroring the existing `GetCashSummaryQueryHandler` test pattern — none
exist yet for reports since the feature doesn't exist).

---

## Phase 3 — User Management (closes a real security gap)

**Why here:** Phase 0 proved auth works; this phase is what makes auth
*operable* rather than a fixed pair of demo accounts. Lower urgency than
Phases 1–2 since it doesn't block a visible SRS wireframe, but it's a real
gap flagged in the last summary.

### Backend
| Capability | Implementation |
|---|---|
| List/create users (Admin only) | `GetUsersQuery`, `CreateUserCommand` — mirrors `CreateCustomerCommand` exactly |
| Change password (self-service) | `ChangePasswordCommand` — requires current password verification via `IPasswordHasher.Verify()`, then `User.ChangePasswordHash()` |
| Deactivate a user | `DeactivateUserCommand` — needs a `UserStatus` concept added to the `User` aggregate (currently has none — add it now, following the same pattern as `CustomerStatus`) |
| `UsersController` | `GET/POST /api/users`, `PUT /api/users/{id}/password`, `POST /api/users/{id}/deactivate` — all Admin-only except password change (self, or Admin for anyone) |

### Frontend
- `presentation/settings/settings.component.ts` — the "Settings" nav item already exists and is unrouted; this is where it goes
- User list (Admin only — hide the whole page for Staff via `authGuard` + role check, same pattern as the Cash & Funds Admin-only button)
- "Change my password" form, available to both roles

**Acceptance criteria:** Admin creates a new Staff user, that user can log
in, Staff cannot see the user list, either role can change their own
password.

**Effort:** 2 days.

---

## Phase 4 — Input Validation Layer (hardening, not a new feature)

**Why here, not earlier:** by this point there are enough commands
(loans, payments, extensions, customers, users) that a systematic
validation layer pays for itself; doing it on 3 commands wasn't worth a
new dependency, doing it on 12 is.

### Backend
- Add `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
- One `AbstractValidator<T>` per command (e.g. `RecordPaymentCommandValidator`: amount > 0, valid payment method enum value, loan id is a parseable GUID)
- A MediatR pipeline behavior (`ValidationBehavior<TRequest, TResponse>`) that runs validators automatically before the handler — this is the one piece of new infrastructure, everything else is per-command validator classes following one template
- Map `FluentValidation.ValidationException` to 400 in the existing `ExceptionHandlingMiddleware` (one more case in the switch expression already there)

**Acceptance criteria:** Sending a malformed request (negative payment
amount, invalid enum string, missing required field) returns a 400 with a
field-level error message, without the request ever reaching a handler.

**Effort:** 1–2 days for the pipeline + first few validators; each
additional command's validator is 15–30 minutes after that.

---

## Phase 5 — CI Pipeline

**Why here:** with four feature phases' worth of new tests accumulated,
manual `dotnet test` runs become the bottleneck this actually solves.
Could move earlier if the team is already collaborating (multiple people
pushing to the same repo) — in that case, move this to right after Phase 0.

### Deliverable
GitHub Actions workflow (`.github/workflows/ci.yml`):
```yaml
- Backend: dotnet restore → dotnet build → dotnet test (Sqlite-backed, no external services needed)
- Frontend: npm ci → ng build → (ng test if/when Angular unit tests exist — none yet, see Phase 6)
```
Both jobs run on every push/PR; backend job is the one that finally
gives continuous, automatic answer to "does this still compile" instead of
relying on a human running it locally.

**Acceptance criteria:** A PR with a deliberately broken test fails CI
visibly; a clean PR passes both jobs.

**Effort:** 0.5 day.

---

## Phase 6 — Frontend Unit Tests + Performance/Load Testing

**Why last:** genuinely lower priority than the above — the frontend has
been verified by real `ng build` at every step so far (catching two real
bugs already), and performance is unproven but also not yet a reported
problem. Both are worth doing before a production launch, not before the
next feature.

### Frontend unit tests
- Karma/Jasmine specs for the use-case layer (mock the repository ports, assert the use case calls the right method) — mirrors the backend's `Application.Tests` pattern
- Component tests for anything with non-trivial logic (`LoanTimelineComponent`'s day-calculation math is the best candidate — it's pure enough to test in isolation and complex enough to be worth it)

### Performance/load testing
- k6 or a similar tool against the seeded dataset scaled up (SRS 4 says "hundreds of loans without slowdown" — write a seed variant that generates 500+ loans and payments, then load-test the dashboard/loans list endpoints)
- Watch specifically for the `GetCashSummaryQueryHandler`'s in-memory
  aggregation over all ledger entries (flagged in the backend README as a
  potential bottleneck "if the ledger ever grows large") — this is the
  first place to look if load testing shows a problem

**Acceptance criteria:** Dashboard loads in <1s with 500 loans and 2,000
ledger entries seeded.

**Effort:** 2–3 days.

---

## Summary timeline

| Phase | Focus | Effort | Blocks later phases? |
|---|---|---|---|
| 0 | Verify existing build | 0.5–3 days | Yes — do not skip |
| 1 | Customers + Profile UI | 2–3 days | No |
| 2 | Reports page | 3–4 days | No |
| 3 | User management | 2 days | No |
| 4 | Input validation | 1–2 days | No |
| 5 | CI pipeline | 0.5 days | No — but cheap, move earlier if multiple people are committing |
| 6 | Frontend tests + load testing | 2–3 days | No |

**Total: roughly 11–18 working days**, depending mostly on how much Phase 0
surfaces and whether Reports needs PDF export.

Phases 1–4 have no dependencies on each other and can be reordered or
parallelized across two people once Phase 0 is clear (e.g., one person on
Customers UI + Reports, another on User Management + Validation).
