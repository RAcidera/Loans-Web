# Implementation Plan — Loan Tracking Additional Functional Requirements

## Current state

This plan covers the requirements in `LoanTrackingAdditionalFunctionalRequirements.pdf`
(sections 3.1–3.4: Customer Management, Loan Management, Loan Extension
Management, Payment Management, plus the Loan Status/Classification split
and Loan Ledger recommendations folded into the document). The backend and
frontend already implement a working subset of the loan lifecycle — Loan
aggregate with a single `LoanStatus` enum, `LoanExtension`/`Payment` child
entities with add-only commands, a `loan-details-dialog` modal, a
client-computed dashboard, and a `Customer` aggregate with the core profile
fields. The gap analysis below (done by reading the actual code, per this
repo's standing convention — the requirements doc and this repo's own
README/TESTING.md are both written ahead of the code, not after it) found
**13 requirement areas, none fully built**: most are partial (an adjacent
field or command already exists) and three are entirely new (customer/loan
document storage, the Loan Status/Classification split, the per-loan
transaction ledger).

I did not add a Phase 0 verification gate. `dotnet build` currently fails,
but only with `MSB3027`/file-lock errors against `bin/Debug` DLLs held open
by a running Visual Studio debug session (`LoanManagementSystem.Api
(16100)`) — an environment artifact, not a code regression. Close Visual
Studio (or stop the running API process) before building; there's no
evidence of an actual unverified/broken code state to gate Phase 1 on.

---

## Guiding principles for every phase

1. **Get the domain model right before building UI on it.** Loan Status vs.
   Loan Classification, and the Extension Charges/Interest split, are read
   by nearly every later phase (dashboard, loan list filters, SOA, customer
   financial summary). Get this wrong in Phase 1 and every later phase
   redoes work.
2. **Follow the existing pattern, don't invent a new one.** Backend feature
   = domain change → Application command/query → EF `IEntityTypeConfiguration`
   (new entity) → controller endpoint. Frontend feature = domain entity →
   use case → component, wired through the existing repository ports.
   Document storage and the transaction ledger both have a near-identical
   precedent already in the codebase (`CashLedgerEntry` + its domain-event
   handler) — reuse that shape rather than designing a new one.
3. **Watch the enum-migration gotcha.** Several phases add a new `NOT NULL`
   enum column (`LoanClassification`, `UserStatus`-style additions). Per
   `CLAUDE.md`, EF's auto-generated `defaultValue` for a `HasConversion<string>()`
   enum comes out as `""`, not the enum's real default member — always open
   the generated migration and fix it before running `database update`.
4. **One phase should be shippable before the next starts.** Each phase
   below ends in a working, demoable increment.

---

## Phase 1 — Loan domain foundation: Status/Classification split, Extension Charges, receivables formulas

**Why first:** this is the highest-leverage, highest-risk change in the
whole document. The dashboard's Gross/Collectible/Bad Loan Receivables
(spec §"Dashboard Receivable Calculations"), the loan list's Classification
column/filters, the Customer screen's financial summary, and the "Change
Classification" button on Loan Details all read off fields that don't
exist yet. Building any of those UIs against today's single `LoanStatus`
enum means rebuilding them once this lands. Backend-only phase — no user-facing
change yet, so it's low-risk to ship first even though it's foundational.

### Backend
| Capability | Implementation |
|---|---|
| Split Loan Status (system) from Loan Classification (user) | New `LoanClassification` enum (`Normal`/`WatchList`/`BadLoan`) on `Loan`, `HasConversion<string>()` per the existing `CustomerStatus` pattern. Keep `LoanStatus` but add the missing `WrittenOff` member (today's enum is Active/Extended/Paid/Overdue only). |
| Written Off is not auto-derivable | Spec labels Loan Status "system managed," but nothing else in the document (balance, due date) implies a "written off" trigger — it's a lender decision to stop pursuing collection. **Judgment call:** treat it as an explicit Admin-only status transition (`WriteOffLoanCommand`), not something `RefreshOverdueStatus` sets automatically. |
| `ChangeLoanClassificationCommand` | Admin/Staff-settable per the "Change Classification" button on Loan Details — no approval workflow implied by the spec, so a direct set. |
| Extension Charges as a distinct amount | `LoanExtension` currently has `AdditionalInterestAmount` only. Add `AdditionalChargesAmount` (spec's extension fields are "Additional Interest" **and** "Additional Charges" — two numbers, not one). Add a `TotalExtensionCharges` accumulator on `Loan`, updated in `Extend()` alongside the existing `TotalInterest` update. |
| Outstanding Balance formula | Update to `Principal + TotalInterest + TotalExtensionCharges - TotalPaid` (today's formula omits the extension-charges term because the field doesn't exist yet). |
| Loan Number format | Change generation/formatting to `LOA` + 5-digit zero-padded running number (`LOA00001`) — replaces today's `LM-001`-style display. |
| Dashboard receivables query | `GetDashboardReceivablesQuery` — Gross Receivables (`SUM(balance)` excluding Written Off), Bad Loan Receivables (`SUM(balance) WHERE Classification = BadLoan`), Collectible Receivables (`Gross - BadLoan`), counts for Active/Overdue/WrittenOff, and "Loans Due This Week" (`DueDate` within the next 7 days, status Active/Overdue). |
| Endpoints | `PUT /api/loans/{id}/classification`, `POST /api/loans/{id}/write-off`, `GET /api/dashboard/receivables`. |

**Acceptance criteria:** A migration adds `LoanClassification` and
`AdditionalChargesAmount`/`TotalExtensionCharges` columns with correct
defaults (verify the generated `defaultValue` per the CLAUDE.md gotcha
before applying); an integration test confirms
`Balance == Principal + Interest + ExtensionCharges - Payments`; a unit
test confirms Gross/Collectible/Bad Loan Receivables math against a
seeded mix of Active/Overdue/WrittenOff/BadLoan loans.

**Effort:** 3–4 days (migration + formula changes are mechanical; the
receivables query needs its own unit tests since nothing like it exists
today).

---

## Phase 2 — Dashboard + Loan List (frontend, consumes Phase 1)

**Why second:** purely a frontend consumer of Phase 1's new query and
fields — the fastest way to make the foundational change visible and
validate it against real data before building the bigger Loan Details
phase on top.

### Frontend
| Component | Purpose |
|---|---|
| `dashboard.component.ts` | Replace the client-computed KPI cards with server-driven ones: Gross Receivables, Collectible Receivables, Bad Loan Receivables, Active/Overdue/Written-Off counts, Loans Due This Week |
| `loans.component.ts`/`.html` | Add Interest Amount, Extension Charges, Total Payments columns; add a footer totals row (Total Principal/Interest/Extension Charges/Payments/Outstanding Balance); add filters (Loan Status, Loan Classification, Loan Date range, Due Date range, "Show Bad Loans Only", "Show Overdue Loans Only") |
| `GetLoansPageQuery` | Extend with `Status`, `Classification`, `LoanDateFrom/To`, `DueDateFrom/To`, `BadLoansOnly`, `OverdueOnly` parameters |

**Acceptance criteria:** Dashboard cards match the Phase 1 query's numbers
against seeded data; filtering the loan list by "Show Bad Loans Only"
returns only `BadLoan`-classified loans; the footer totals row sums match
the visible page (or the full filtered set, whichever this repo's existing
pagination convention uses — check `GetCashSummaryQueryHandler`'s footer
pattern if one exists).

**Effort:** 2–3 days.

---

## Phase 3 — Loan Details page rebuild (routed page, not modal)

**Why third:** the single biggest UI change in the document — the spec is
explicit ("Do not use modal dialogs... payment and extension history can
become large") — and several other requirements (Generate SOA, Change
Classification, Audit Log) hang off buttons/tabs on this page. Needs Phase
1's classification field to exist for the "Change Classification" action.
Independent of Phase 2, so these two can run in parallel if resourced.

### Backend
| Capability | Implementation |
|---|---|
| Edit loan | `UpdateLoanCommand` — allows overriding Loan Date, Due Date, Interest Rate, Interest Amount, and a new `Remarks` field post-creation (spec: "the lender may provide a goodwill discount... must allow editing of these fields after loan creation"). `Loan` currently has no `Remarks` field or edit-after-creation path (only `Extend()`) — add both. |
| Edit/Delete payment | `UpdatePaymentCommand`, `DeletePaymentCommand` — `RecordPaymentCommand` is currently the only one; both new commands must recalculate `Balance`/`Status` the same way `RecordPayment` does today |
| Edit/Delete extension | `UpdateExtensionCommand`, `DeleteExtensionCommand` — same story; deleting/editing an extension must roll back its contribution to `TotalInterest`/`TotalExtensionCharges`/`DueDate` |
| Payment Method "Other" | Add to `PaymentMethod` enum (currently Cash/GCash/BankTransfer only) |
| Reference Number | Add to `Payment` — spec lists it as a payment field, not present today |

### Frontend
| Component | Purpose |
|---|---|
| `presentation/loan-details/loan-details.component.ts` | New routed page at `/loans/:id`, replacing `loan-details-dialog` as the primary entry point (dashboard and loans-list "click a row" now navigate here instead of opening the dialog) |
| Header | Loan #, Customer, Principal, Outstanding Balance, Due Date, Status — plus action buttons: Edit Loan, New Payment, New Extension, Generate SOA (stub until Phase 7), Change Classification |
| Tabs: Overview | Loan Date, Due Date, Principal, Interest Rate, Interest Amount, Total Extension Charges, Total Payments, Outstanding Balance, Loan Status, Loan Classification |
| Tabs: Payments | Existing payments table + Edit/Delete actions + footer total |
| Tabs: Extensions | Existing extensions table + Edit/Delete actions + footer total |
| Tabs: Documents, Audit Log | Stub/placeholder tabs in this phase — filled in by Phase 5 and Phase 6 respectively, so the tab shell exists once and isn't rebuilt |
| `edit-loan-dialog`, updates to `add-payment-dialog`/`extend-loan-dialog` | Add Remarks/override fields, Reference Number, "Other" payment method |

**Acceptance criteria:** Clicking a loan anywhere in the app navigates to
`/loans/:id`, not a modal; editing a payment updates the balance and Payments
tab immediately; deleting an extension reverts the due date and extension
charges; changing classification from the header persists and reflects on
the dashboard's Bad Loan Receivables figure.

**Effort:** 5–6 days — this is the largest phase; the dialog-to-routed-page
migration touches every place that currently opens `loan-details-dialog`.

---

## Phase 4 — Customer core field additions

**Why here:** small, independent domain change — no dependency on Phases
1–3, so it can run in parallel with any of them if a second person is
available. Placed before Phase 8 (Customer Profile screen) since that
phase displays these fields.

### Backend
| Capability | Implementation |
|---|---|
| Customer Code (auto-generated) | Same sequential-number-plus-prefix pattern as the new Loan Number format from Phase 1, scoped to `Customer` |
| Nickname/Alias, Notes | New nullable fields on `Customer`, following the existing `FullName`/`Address` pattern in `Customer.cs` |
| `UpdateCustomerCommand` extension | Existing edit command (added in the prior `loan-management-implementation-plan.md` Phase 1) needs these two new fields added to its payload |

**Acceptance criteria:** Creating a customer auto-assigns a Customer Code
visible in the list and profile; editing Nickname/Notes persists and
displays.

**Effort:** 1–1.5 days.

---

## Phase 5 — Document Management (Customer + Loan)

**Why here:** entirely new aggregate-adjacent entity with no dependency on
Phases 1–4's domain changes, so the backend half can start immediately in
parallel with Phases 2–4. The frontend half needs Phase 3's Documents tab
shell (Loan side) to exist, and the existing `customer-profile` component
(Customer side) — both are just slots to fill by this point.

### Backend
| Capability | Implementation |
|---|---|
| Document storage | Two child entities — `CustomerDocument` and `LoanDocument` — mirroring the existing pattern of `Payment`/`LoanExtension` as owned child collections, not a single polymorphic "Document" table. Both store the file as `VARBINARY(MAX)` directly in SQL Server (spec is explicit: no server filesystem storage) plus metadata (Document ID, Original File Name, File Type, File Size, Upload Date, Uploaded By) |
| Shared upload/validation logic | A small shared service (`IDocumentStorageService` or similar) validating file type (JPG/PNG/PDF only) and enforcing a size limit before either entity accepts a file — avoids duplicating validation across two commands |
| Commands | `UploadCustomerDocumentCommand`, `DeleteCustomerDocumentCommand`, `UploadLoanDocumentCommand`, `DeleteLoanDocumentCommand` |
| Endpoints | `POST/GET/DELETE /api/customers/{id}/documents`, same shape for `/api/loans/{id}/documents`; download returns the raw bytes with the stored content-type/filename |

### Frontend
| Component | Purpose |
|---|---|
| A shared `document-upload` presentational component (multi-file picker, JPG/PNG/PDF accept filter) | Reused on both the Customer Documents tab and the Loan Details Documents tab, per this repo's SCSS-duplication-over-shared-styles convention but genuine logic reuse for the upload widget itself |
| Wiring into `customer-profile` and `loan-details` Documents tabs | List with View/Download/Delete actions |

**Acceptance criteria:** Upload a PDF and a PNG to a customer, see both
listed with correct size/date/uploader, download one back byte-for-byte,
delete the other; same flow works from a Loan Details page.

**Effort:** 3–4 days (two owning aggregates, but the storage/validation
logic is written once and reused).

---

## Phase 6 — Loan Transaction Ledger + Audit Log tab

**Why here:** needs Phase 3's Loan Details page (specifically the Audit
Log tab slot) and Phase 1's classification/write-off actions (new
domain events to log) in place first. The ledger is the spec's own
"Additional Recommendation," motivated by "accurate SOA generation" and
"prevents balance calculation errors" — sequencing it right before Phase 7
(SOA) means the PDF is built against an authoritative debit/credit/balance
trail instead of re-deriving running balances ad hoc from Payments +
Extensions collections, which is how the code works today.

### Backend
| Capability | Implementation |
|---|---|
| `LoanLedgerEntry` | New entity: Date, Transaction type (Loan Released / Interest Added / Extension / Payment), Debit, Credit, Running Balance — populated via a domain-event handler off `LoanCreatedDomainEvent`, `PaymentRecordedDomainEvent`, `LoanExtendedDomainEvent`, following the exact pattern `CashLedgerEntry`'s handler already uses in this codebase (separate `SaveChangesAsync`, same non-atomic trade-off documented in `CLAUDE.md` — acceptable at this scale) |
| Audit Log entries | New `LoanAuditLogEntry` (or reuse the ledger table with a "non-financial" transaction type) capturing loan edits, classification changes, and write-offs with who/when — needs new domain events (`LoanEditedDomainEvent`, `LoanClassificationChangedDomainEvent`, `LoanWrittenOffDomainEvent`) raised from the Phase 3/1 commands |
| Backfill | One-time script/migration step to generate ledger entries for existing loans' payment/extension history, so the ledger isn't empty for pre-existing data |

### Frontend
- Fill in the Loan Details "Audit Log" tab (chronological list) and give
  the Payments/Extensions tabs a Running Balance column sourced from the
  ledger instead of computed client-side.

**Acceptance criteria:** Every payment/extension/write-off/classification
change produces a ledger or audit entry within the same request cycle;
the Payments tab's Running Balance column matches the ledger's.

**Effort:** 3 days.

---

## Phase 7 — Statement of Account (SOA) PDF generation

**Why here:** depends on Phase 1 (status/classification for the summary
section), Phase 3 (the "Generate SOA" button already wired to a stub), and
Phase 6 (ledger-backed running balances for the Payment/Extension History
sections) — building this earlier would mean generating the PDF off
less-reliable ad hoc balance math and redoing it once the ledger lands.

### Backend
| Capability | Implementation |
|---|---|
| PDF library | No PDF generation exists anywhere in the backend today (confirmed — no `Pdf` references in any `.csproj`). Add QuestPDF (or similar), same "new dependency, budget extra time" note the prior plan gave CSV-vs-PDF reporting |
| `GenerateLoanSoaQuery` | Assembles Customer Info, Loan Info, Extension History, Payment History (all from the Phase 6 ledger), and Summary (Principal/Interest/Extension Charges/Total Due/Total Payments/Outstanding Balance/Status/Classification) into a PDF document |
| Endpoint | `GET /api/loans/{id}/soa` returning `application/pdf` |

### Frontend
- Wire the Phase 3 "Generate SOA" button to trigger the download.

**Acceptance criteria:** Generate SOA on a loan with at least one payment
and one extension; the PDF opens correctly and its summary numbers match
the Overview tab exactly.

**Effort:** 2–3 days.

---

## Phase 8 — Customer Profile screen: financial summary + tabs

**Why last:** needs Phase 1 (per-customer receivables — a customer-scoped
variant of the Phase 1 dashboard query), Phase 4 (the new Customer fields
to display), and Phase 5 (the Documents tab) all in place — this phase is
mostly assembly of prior phases' pieces into the spec's recommended layout,
not new domain logic.

### Backend
| Capability | Implementation |
|---|---|
| Per-customer receivables | `GetCustomerReceivablesQuery` — same Gross/Collectible/Bad Loan Receivables formulas as Phase 1's dashboard query, filtered to one customer's loans |

### Frontend
| Component | Purpose |
|---|---|
| `customer-profile.component.html` restructure | Sections: Customer Information card, Financial Summary card (Active Loans, Gross/Collectible/Bad Loan Receivables), then `Loans \| Documents \| Notes` tabs (currently a flat profile + loans table with no tab structure) |
| Loans tab | Existing loans table, unchanged, now navigates to the Phase 3 routed Loan Details page on click instead of opening a dialog |
| Documents tab | Phase 5's shared upload component |
| Notes tab | Free-text notes field/history — spec lists "Notes" as both a Customer field (Phase 4) and a tab; use the tab for a note-taking UI if there's meant to be more than one note over time, otherwise this can just surface the single `Notes` field from Phase 4 in read/edit form |

**Acceptance criteria:** Open a customer profile, see the financial summary
numbers match what the dashboard would show if filtered to just that
customer; switch between Loans/Documents/Notes tabs without a page reload.

**Effort:** 2–3 days.

---

## Summary timeline

| Phase | Focus | Effort | Depends on |
|---|---|---|---|
| 1 | Loan Status/Classification split, Extension Charges, receivables formulas | 3–4 days | — |
| 2 | Dashboard + Loan List frontend | 2–3 days | 1 |
| 3 | Loan Details page rebuild (routed, tabs, edit/delete commands) | 5–6 days | 1 |
| 4 | Customer core fields (Code/Nickname/Notes) | 1–1.5 days | — |
| 5 | Document Management (Customer + Loan) | 3–4 days | 3 (frontend slot only) |
| 6 | Loan Transaction Ledger + Audit Log tab | 3 days | 1, 3 |
| 7 | Statement of Account PDF | 2–3 days | 1, 3, 6 |
| 8 | Customer Profile financial summary + tabs | 2–3 days | 1, 4, 5 |

**Total: roughly 21–28 working days.**

Phase 1 is the one true blocker — everything downstream reads its new
fields/queries. After Phase 1 clears, Phases 2, 3, and 4 have no
dependencies on each other and can be parallelized across two or three
people (e.g., one person on Dashboard+Loan List, another on the Loan
Details rebuild, a third on Customer fields + starting Document Management's
backend). Phases 6–8 are more sequential since each leans on the previous
phase's tab shells or data.
