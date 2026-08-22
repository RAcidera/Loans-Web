# Implementation Plan — Diary, Calendar, Financial Snapshot & Compare-to-Today

## Current state

The backend and frontend are both further along than the last plan
(`loan-management-implementation-plan.md`) implies: Customers, Loans,
Payments, Reports, Cash & Funds, and Settings are all built and routed
(`app.routes.ts:16-36`, `admin-shell.component.ts:48-56`), and dashboard
financial aggregation already exists (`GetDashboardSummaryQueryHandler`,
`GetCashSummaryQueryHandler`). None of Diary, Calendar, Financial
Snapshot, or Promise-to-Pay exist anywhere yet — a repo-wide grep for
`Diary|Calendar|PromiseToPay|Journal|Snapshot` turns up nothing outside
the requirements doc itself and unrelated EF/model-snapshot noise. This
module is entirely greenfield, layered on top of a codebase that's
otherwise already known-green, so there's no Phase 0 build-verification
gate here (see "Phase 0" note below).

Source requirements: `diary-calendar-financial-snapshot-requirements.md`
(cited below as "§N").

---

## Guiding principles for every phase

1. **Reuse the calculation, don't re-derive it.** §9 is explicit: the
   Diary snapshot must not duplicate financial formulas. Gross/Collectible/
   Bad Loan Receivables and Cash on Hand already have a correct
   implementation in `GetDashboardSummaryQueryHandler` (`ComputeReceivables`,
   `src\LoanManagementSystem.Application\Loans\Queries\GetDashboardSummary\GetDashboardSummaryQuery.cs:80-85`)
   and `GetCashSummaryQueryHandler`
   (`src\LoanManagementSystem.Application\CashLedger\Queries\GetCashSummary\GetCashSummaryQuery.cs:16-66`).
   Extract those into a shared calculation service both the Dashboard and
   the new snapshot service call — don't hand-copy the formulas into Diary.
2. **The snapshot is server-authoritative and immutable, permanently.**
   §7 and §10 are the entire point of this module — if snapshot values can
   drift or be Angular-computed, the "compare six months later" feature is
   meaningless. Treat this as a first-class constraint checked in every
   phase that touches `DiaryFinancialSnapshot`, not just Phase 1.
3. **Follow the existing pattern, don't invent a new one.** Backend:
   domain aggregate → application command/query → EF config → controller,
   same as Loans/Customers. Audit trail: mirror `LoanAuditLogEntry`
   (`src\LoanManagementSystem.Domain\Loans\LoanAuditLogEntry.cs`) — its
   own small aggregate, populated from domain-event handlers reacting to
   events raised on the parent aggregate, `PerformedBy` as a plain
   username string (no user-id FK exists anywhere in this codebase, so
   don't introduce one here). Frontend: domain entity → use case →
   component, wired through a new dedicated repository port per CLAUDE.md's
   "one port per lifecycle boundary" rule (Diary is its own boundary,
   separate from Loans/Customers).
4. **Diary and Calendar are integrated by design, not two unrelated
   features (§4).** Every phase that adds a new event source (diary
   reminders, loan due dates, promises) should leave it visible on the
   Calendar by the end of that phase, not as a later wiring step.
5. **One phase shippable before the next starts.** Each phase below ends
   in something a user can actually click through, not a partial data
   model.

**Phase 0 — skipped.** The rest of the app is already built, routed, and
presumably exercised (Reports/Settings/Customers all exist per the gap
analysis) — there's no fresh-clone/never-built state to gate on here the
way the original plan's Phase 0 addressed. If `dotnet test`/`ng build`
aren't currently green, run them before starting Phase 1, but that's a
one-off check, not a phase.

---

## Phase 1 — Diary Entry + Categories + Financial Snapshot + Compare to Today (§4–17, §27 majority)

**Why first:** §28 states this outright — "Highest priority: Diary Entry
+ Financial Snapshot + Compare to Today." It's also the phase with the
only genuinely new backend logic (the shared calculation extraction);
Calendar and Promise-to-Pay in later phases are comparatively mechanical
once this exists.

### Backend

| Capability | Implementation |
|---|---|
| Shared financial calculation, extracted once | Pull the receivables/cash/count math out of `GetDashboardSummaryQueryHandler`/`GetCashSummaryQueryHandler` into a shared `Application/Common` service (e.g. `IFinancialCalculationService`) callable from both the existing Dashboard handler and the new snapshot service — satisfies §9's "reuse the same calculation services" without duplicating formulas |
| Collections Today/MTD, Loan Releases Today/MTD | New — nothing in the current Dashboard/Reports code computes "today" or "released today" (only monthly/last-7-days buckets exist, `GetDashboardSummaryQuery.cs:103-134`). Add as new methods on the same shared calculation service so Dashboard can reuse them later too |
| `DiaryCategory` (Domain aggregate) | `Id, Name, Icon, DisplayColor, IsActive, SortOrder` per §5. No existing lookup-entity pattern to mirror in this codebase (confirmed via gap analysis) — this is a new small aggregate + EF config. Seed the 10 initial categories (§5) via `DbSeeder`, following its existing idempotent-seed pattern |
| `DiaryEntry` (Domain aggregate) | Fields per §4. Raises `DiaryEntryCreatedDomainEvent`/`DiaryEntryUpdatedDomainEvent`/`DiaryEntryDeletedDomainEvent`/`FinancialSnapshotCapturedDomainEvent` for audit (§24), following the same `AppDbContext.SaveChangesAsync` collect-then-`_mediator.Publish` pattern already used for `Loan` (`AppDbContext.cs:72-90`) |
| `DiaryFinancialSnapshot` | Child of `DiaryEntry`, fields per §8. Written once at creation via `IFinancialCalculationService`, never recalculated on read or on `DiaryEntry` edit — enforce this at the aggregate level (no setter path from `UpdateDiaryEntry` reaches the snapshot) |
| `DiaryAuditLogEntry` | Mirrors `LoanAuditLogEntry` exactly: own repository, populated by event handlers reacting to the four events above, covers the §24 audit list (Created/Updated/Deleted/Snapshot Captured/Reminder Changed/Linked Customer Changed/Linked Loan Changed) |
| `IFinancialSnapshotService` / `FinancialSnapshotService` | Per §21: `CaptureCurrentSnapshot()` (called from `CreateDiaryEntryCommand` when the checkbox is set), `GetCurrentFinancialPosition()`, `CompareSnapshotToCurrent()` — all delegate to the shared calculation service from row 1 |
| Commands/Queries | `CreateDiaryEntryCommand`, `UpdateDiaryEntryCommand` (title/notes/category/customer/loan/reminder only — never touches the snapshot), `DeleteDiaryEntryCommand`, `GetDiaryEntryQuery`, `SearchDiaryEntriesQuery` (filters per §12: search text across Title/Notes/Customer Name/Customer Code/Loan Number, Category, Date range, Customer, Loan, Has Snapshot, Has Reminder), `GetDiaryFinancialSnapshotQuery`, `CompareSnapshotToCurrentQuery` (Change/% per §16, zero-snapshot → `N/A`/`New` per §16) |
| `DiaryController` | Endpoints per §22: `GET/POST /api/diary`, `GET/PUT/DELETE /api/diary/{id}`, `GET /api/diary/{id}/snapshot`, `GET /api/diary/{id}/compare-to-today` |
| `DiaryCategoriesController` | `GET /api/diary-categories` (active, sorted) for the dropdown — full CRUD admin UI for categories is not in any wireframe in the requirements doc; treat as out of scope for this phase unless the user asks for it explicitly, and flag that assumption when this phase ships |

### Frontend

| Component | Purpose |
|---|---|
| `domain/entities/diary-entry.entity.ts`, `diary-category.entity.ts`, `financial-snapshot.entity.ts` | New entities |
| `domain/repositories/diary.repository.ts` (port) + `infrastructure/http-diary.repository.ts` | New dedicated port per CLAUDE.md's "one port per lifecycle boundary" — Diary is its own boundary, not folded into `LoanRepository`. Bind in `app.config.ts` |
| `application/use-cases/` | `create-diary-entry`, `update-diary-entry`, `delete-diary-entry`, `get-diary-entry`, `search-diary-entries`, `get-diary-snapshot`, `compare-snapshot-to-today`, `get-diary-categories` — each ~10 lines, mirroring existing use cases |
| `presentation/diary-list/` | Chronological timeline, **not** a data grid (§11 is explicit) — grouped by day, sorted `EntryDateTime DESC`, search/category/date-range filter bar, inline snapshot summary + `[View Snapshot] [Edit] [⋮]` per entry |
| `presentation/diary-form/` | Create/edit dialog or page per §6 layout — Title, Category dropdown, Date/Time (defaults to now, overridable), Notes, "Capture current financial snapshot" checkbox (default off), optional Customer/Loan link, optional reminder date/time |
| `presentation/diary-detail/` | **Dedicated page, not a modal** (§25) — header, notes, financial snapshot (§14 layout), Compare to Today, reminder, audit info |
| `presentation/financial-snapshot/` | Reusable snapshot display block (§14) |
| `presentation/financial-comparison/` | Snapshot vs Today vs Change vs % table (§15), contextual coloring per §17 (bad loans/overdue down = green, up = red; cash up = green; gross receivables = neutral purple/blue) |
| `presentation/category-badge/` | Reusable chip reading `DisplayColor`/`Icon` from `DiaryCategory` — §5 explicitly forbids hardcoding category colors in the Angular app |
| Nav + routes | Add "Diary" to `admin-shell.component.ts` `navItems` (journal/notebook icon per §3) and `/diary`, `/diary/new`, `/diary/:id` to `app.routes.ts` |

**Acceptance criteria:** Create a diary entry with the snapshot checkbox
on; the saved figures match what Dashboard shows at that instant. Record
a new payment/loan afterward, reopen the same diary entry, and the
snapshot is unchanged. Edit the entry's title/notes/category — snapshot
still unchanged. Click Compare to Today and see Snapshot/Today/Change/%
with correct signs, correct contextual colors, and `N/A`/`New` for any
zero-snapshot metric. Search and filter the timeline by each filter in
§12.

**Effort:** 6–8 days — the largest phase: two new aggregates, a new
shared calculation extraction, full CRUD + search + timeline UI, and the
comparison feature.

---

## Phase 2 — Calendar (Month/Week/Day) with Diary Reminders + Loan/Extension Due Dates (§18, §19, §25 calendar rules)

**Why second:** two of the four event sources (loan due dates, extension
due dates) need no new domain logic — they're read-only queries against
the existing `Loan` aggregate's due-date fields. The third (diary
reminders) now exists because Phase 1 shipped `DiaryEntry.ReminderDate`.
Only Promise-to-Pay is deferred, because that aggregate doesn't exist
yet — its calendar slot is stubbed here and wired live in Phase 3.

### Backend

| Capability | Implementation |
|---|---|
| `ICalendarService`/`CalendarService` | Per §21: `GetEvents(fromDate, toDate)` fans out to `GetDiaryEvents()` (diary entries + reminders in range, via `IDiaryRepository`), `GetLoanDueEvents()`/`GetExtensionDueEvents()` (new lightweight read query against the existing `Loan` repository — no domain changes needed), `GetPromiseToPayEvents()` (stub returning empty until Phase 3) |
| Unified `CalendarEventDto` | `Id, Type, Title, Date, Time?, Color, LinkedEntityType, LinkedEntityId` — one shape regardless of source, so the frontend renders generically and "navigate to the related record" (§27 acceptance item) is a single click-through helper keyed on `Type` |
| `CalendarController` | `GET /api/calendar/events?from=&to=&types=` per §22 |

### Frontend

| Component | Purpose |
|---|---|
| `presentation/calendar-page/` | Month/Week/Day toggle (default Month per §18), Today/Prev/Next controls, event-type toggle checkboxes (§19) |
| `presentation/calendar-event/` | Reusable event pill/chip, colored by source type or diary category |
| `presentation/calendar-event-detail/` | Click-through: loan/extension due → loan detail; diary/reminder → diary detail; promise → promise detail (once Phase 3 exists) |
| "+N more" overflow | Month cells cap visible events and collapse the rest to `+3 more` (§25) — this and the readable-with-many-events requirement drive most of the layout work |

**Decision made in this plan:** build a custom Month/Week/Day grid rather
than adding a calendar npm package. `package.json` currently has no
calendar library (`angular-calendar`, FullCalendar, etc.) and only
`@angular/material`'s (unused) `MatDatepickerModule` is available. A
custom grid avoids a new dependency and gives exact control over the
"+3 more" overflow and category-color rendering the requirements specify
precisely — flag this as a judgment call if the user has a library
preference.

**Acceptance criteria:** Switch between Month/Week/Day and see loan due
dates, extension due dates, and diary reminders rendered on the correct
days; a day with more events than the visible max collapses to
`+N more`; clicking any event navigates to its source record.

**Effort:** 4–5 days — the custom calendar grid (three view modes,
overflow handling, responsive) is most of this.

---

## Phase 3 — Promise to Pay + live Calendar integration (§20, §22–24 promise portions)

**Why third:** the aggregate and CRUD are independent of Diary/Calendar,
but its calendar surface depends on the event-type toggle and
`CalendarEventDto` plumbing Phase 2 already built — bolting the fourth
event source onto a finished `CalendarService` is cheaper than threading
it through mid-build.

### Backend

| Capability | Implementation |
|---|---|
| `PromiseToPay` (Domain aggregate) | Fields per §20; `Status` enum (`Pending, Kept, Missed, Rescheduled, Cancelled`) |
| `PromiseAuditLogEntry` | Mirrors `LoanAuditLogEntry`/`DiaryAuditLogEntry` again — covers the six promise audit actions in §24 |
| Commands | `CreatePromiseCommand`, `UpdatePromiseCommand`, `DeletePromiseCommand`, plus explicit status-transition commands matching the requirement's own verbs — `MarkPromiseKeptCommand`, `MarkPromiseMissedCommand`, `ReschedulePromiseCommand`, `CancelPromiseCommand` — each raising its own audit event rather than a generic "status changed" |
| `PromisesToPayController` | Endpoints per §22 |
| `CalendarService.GetPromiseToPayEvents()` | Un-stub from Phase 2 — now returns real data |

### Frontend

| Component | Purpose |
|---|---|
| `presentation/promise-form/`, `presentation/promise-detail/` | Per §23 — no standalone promise list page is wireframed in the requirements; surface these from the Customer/Loan detail page (a "Promises" section/tab) rather than adding a new top-level nav item, since payments being irregular is framed as a customer/loan-level concern, not its own module. Flag this as an assumption to confirm if the user expects a dedicated Promises list page |
| Calendar toggle for Promise-to-Pay | Now shows live data instead of the Phase 2 stub |

**Acceptance criteria:** Create a promise-to-pay against a customer/loan;
it appears on the Calendar on its promise date; mark it
Kept/Missed/Rescheduled/Cancelled and see the status (and calendar
rendering) update; each transition is recorded in the audit trail.

**Effort:** 3–4 days.

---

## Phase 4 — Backlog / optional (§8 "optional future fields", §28 Phase 4)

Not committed scope — the requirements doc itself labels this optional.
Listed here only so it isn't lost:

- Automatic daily financial snapshots (a scheduled job capturing a
  snapshot with no diary note attached — needs either a nullable
  `DiaryEntryId` on `DiaryFinancialSnapshot` or a separate lighter
  `DailySnapshot` table; decide which once this is actually requested)
- Historical analytics / trend charts over accumulated snapshots
- Optional future snapshot fields from §8 (`TotalCustomers`,
  `LoansDueThisWeek`, `AverageOutstandingBalance`, etc.)
- Full admin CRUD UI for `DiaryCategory` (Phase 1 only ships seeded
  categories + a read-only dropdown, per the assumption noted there)

Do not scope or estimate this until Phases 1–3 have shipped and the user
confirms it's wanted.

---

## Summary timeline

| Phase | Focus | Effort | Depends on |
|---|---|---|---|
| 1 | Diary CRUD + Categories + Financial Snapshot + Compare to Today | 6–8 days | Nothing new — reuses existing Dashboard/Cash calc logic |
| 2 | Calendar (Month/Week/Day) + Diary reminders + Loan/Extension due dates | 4–5 days | Phase 1 (diary reminders as an event source) |
| 3 | Promise to Pay + live Calendar integration | 3–4 days | Phase 2 (calendar event-type plumbing) |
| 4 | Backlog: auto snapshots, trend analytics, category admin UI | Not estimated | Phases 1–3 |

**Total for committed scope (Phases 1–3): roughly 13–17 working days.**

Phases 1→2→3 have a real dependency chain (each phase's event source
needs the previous phase's aggregate to exist) and should not be
parallelized in the way the original plan's Phases 1–4 could be — this
module is more linear than that one. Within Phase 1, however, the
backend calculation-extraction work and the frontend timeline/detail UI
can proceed in parallel across two people once the `DiaryEntry`/
`DiaryFinancialSnapshot` schema is agreed, since the frontend can build
against a stubbed API response shape first.
