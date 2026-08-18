# Diary / Journal Module – Implementation Requirements

## 1\. Goal

Implement the approved modern Diary / Journal module for the lending application.

The Diary should act as a business journal where the user can:

* Record important events and observations.
* Link entries to customers and loans.
* Capture immutable financial snapshots.
* Compare historical snapshots with current figures.
* Create reminders and follow-up items.
* Record promises to pay.
* Review entries chronologically.
* Integrate with the Calendar module.

## 2\. Main Page Layout

Use this structure:

1. Page header.
2. Compact filter toolbar.
3. Summary cards.
4. Main two-column area:

   * Left: Diary timeline.
   * Right: supporting panels.
5. Responsive tablet/mobile behavior.

Desktop proportions:

* Timeline: 75–80%
* Right sidebar: 20–25%

## 3\. Header

Show:
Diary / Journal
Record important events, notes and financial snapshots.

Primary action:
+ New Entry

Optional dropdown:
Diary Entry
Follow-up / Reminder
Promise to Pay


## 4\. Filters

Compact filter card with:

Search Diary
Category
Date From
Date To
Customer
Loan
Clear
Apply


Search must match:

Title
Notes
Tags
Customer Name
Customer Code
Loan Number
Customer and Loan should be searchable dropdowns.

If Customer is selected, optionally limit Loan choices to that customer.

## 5\. Summary Cards

Show:

Total Entries
This Month
Collections MTD
Loan Due
Reminders


Example:


24 Total Entries
8 This Month
₱73,000 Collections MTD
8 Loan Due
5 Pending Reminders


All financial values must be calculated server-side.

## 6\. Timeline

Do not use a dense table.

Group entries by date:


TODAY — AUGUST 18, 2026
YESTERDAY — AUGUST 17, 2026
AUGUST 16, 2026


Sort:


EntryDateTime DESC


Each item should support:

* timeline dot
* time
* category badge
* icon
* title
* notes preview
* tags
* contextual business information
* financial snapshot preview
* View button
* More menu

## 7\. Entry Card

Example:


14:14   \[Receivables]

Collections improved today

Several overdue customers made payments.
Good sign for this week.

\[collection] \[improvement] \[overdue]

FINANCIAL SNAPSHOT
Gross Receivables       ₱88,086
Collectible             ₱25,359
Bad Loan Receivables    ₱62,727
Cash on Hand           -₱64,189

\[View]


Keep cards compact and avoid large empty areas.

## 8\. Categories

Initial categories:

```text
General
Collections
Receivables
Customer
Loan
Extension
Follow-up
Promise to Pay
Bad Loan
Cash
Important Event
Other


Category entity:


Id
Name
Icon
DisplayColor
IsActive
SortOrder


Suggested colors:


General         Gray
Collections     Green
Receivables     Purple
Customer        Blue
Loan            Blue
Extension       Orange
Follow-up       Green
Promise to Pay  Teal
Bad Loan        Red
Cash            Green
Important Event Purple


Never rely on color alone. Always show category text.



## 9\. Diary Form

Required:


Title
Category
Entry Date
Entry Time
Notes


Optional:


Tags
Customer
Loan
Reminder
Capture Financial Snapshot


Defaults:


Entry Date = Current Date
Entry Time = Current Time


Both dates/times must remain editable.

## 10\. Financial Snapshot

Checkbox:


Capture current financial snapshot


When enabled:

1. Angular sends the Diary entry request.
2. Backend calculates current business figures.
3. Backend stores the snapshot.
4. Snapshot becomes immutable.

Angular must never provide authoritative financial totals.

## 11\. Snapshot Fields

Use:
DiaryFinancialSnapshot


Fields:

Id
DiaryEntryId
GrossReceivables
CollectibleReceivables
BadLoanReceivables
CashOnHand
ActiveLoanCount
OverdueLoanCount
BadLoanCount
CollectionsToday
CollectionsMonthToDate
LoanReleasesToday
LoanReleasesMonthToDate
SnapshotDateTime


## 12\. Snapshot Preview

In Timeline show only key figures:

Gross Receivables
Collectible Receivables
Bad Loan Receivables
Cash on Hand


Optionally:

Collections Today


Provide:


View Snapshot


## 13\. Diary Detail Page

Use a dedicated page, not a small popup.

Sections:


Entry Header
Title
Category
Date / Time
Linked Customer
Linked Loan
Reminder

Notes

Financial Snapshot
Compare to Today

Related Activity
Audit Information
```

## 14\. Compare to Today

Provide:


Compare to Today


Backend compares current financial values against stored snapshot.

Example:


Metric                   Snapshot     Today       Change      %

Gross Receivables        ₱88,086      ₱96,500     +₱8,414    +9.6%
Collectible              ₱25,359      ₱32,400     +₱7,041   +27.8%
Bad Loan Receivables     ₱62,727      ₱58,100     -₱4,627    -7.4%
Cash on Hand            -₱64,189     -₱42,000    +₱22,189
Active Loans                   5            7          +2
Overdue Loans                  3            2          -1
Bad Loans                      2            2           0


Formula:


Change = Current - Snapshot
Percentage Change = (Change / Snapshot) × 100


If Snapshot = 0:


Percentage = N/A


## 15\. Comparison Semantics

Use green only for clearly favorable changes:


Cash on Hand increased
Bad Loan Receivables decreased
Overdue Loans decreased


Use red for clearly unfavorable changes:


Bad Loan Receivables increased
Overdue Loans increased


Use neutral purple/blue for informational changes such as:


Gross Receivables
Active Loans


## 16\. Promise-to-Pay Cards

Example:


10:32 AM \[Promise to Pay]

Promise from Jun Dela Cruz

Jun promised to pay ₱1,000 on Aug 22 after market day.

Customer: Jun Dela Cruz
Loan: LOA00015
Promise Date: Aug 22, 2026
Amount: ₱1,000.00


Statuses:


Pending
Kept
Missed
Rescheduled
Cancelled


## 17\. Follow-up Cards

Example:


04:45 PM \[Loan Due]

Loan follow-up

Called Maria Santos regarding her due loan LOA00005.

Customer: Maria Santos
Loan: LOA00005
Due Date: Aug 20, 2026
Amount Due: ₱3,210.00


## 18\. Extension Cards

Example:


Extension granted

Granted 30-day extension to Pedro Garcia.

Customer: Pedro Garcia
Loan: LOA00012
New Due Date: Sep 16, 2026
Extension Interest: ₱1,250.00


## 19\. Entry More Menu

Provide:


View
Edit
Delete


Optional:


Create Follow-up
Add Reminder
Duplicate


## 20\. Right Sidebar

### Mini Calendar

Show current month.

Requirements:

* Highlight today.
* Mark dates with Diary entries.
* Mark dates with reminders.
* Clicking a date filters Timeline to that date.

### Categories

Show counts:


All Categories   24
General           6
Receivables       4
Loan              5
Extension         2
Follow-up         4
Promise to Pay    3


Clicking a category filters the Timeline.

### Quick Summary

Show:


Collections Today
Collections MTD
Loans Released Today
Loans Released MTD


## 21\. Responsive Behavior

Desktop:


Timeline 75–80%
Sidebar 20–25%


Tablet:

* Sidebar can move below Timeline.

Mobile:


Header
Filters
Summary cards
Timeline
Mini Calendar
Categories
Quick Summary


Do not keep a narrow right sidebar on mobile.

## 22\. Pagination

Recommended default:


20 entries


Options:


20
50
100


Server-side pagination recommended.

Infinite scroll is acceptable if implemented cleanly.

## 23\. Suggested Angular Components


diary-page
diary-header
diary-filters
diary-summary
diary-timeline
diary-date-group
diary-entry-card
diary-entry-context
diary-snapshot-preview
diary-mini-calendar
diary-category-summary
diary-quick-summary
diary-form
diary-detail
financial-snapshot
financial-comparison


Services:

DiaryService
FinancialSnapshotService
PromiseToPayService


Avoid one oversized component.



## 24\. Financial Integrity

Snapshot values must be calculated only by backend services.

Never trust Angular values for:


GrossReceivables
CollectibleReceivables
BadLoanReceivables
CashOnHand
CollectionsToday
CollectionsMonthToDate
LoanReleasesToday
LoanReleasesMonthToDate


Snapshot must reconcile with Dashboard values at capture time.

## 25\. Snapshot Immutability

Editing:


Title
Notes
Category
Customer
Loan
Reminder


must never change an existing snapshot.

If snapshot regeneration is ever added, it must be an explicit audited action.



Store old/new values where relevant.

## 26\. Empty States

No data:


No diary entries yet.

Record important collections, customer events,
financial snapshots, or business observations.

\[ + New Entry ]
```

No filtered results:


No entries match your filters.

\[ Clear Filters ]


## 27\. UX Requirements

* Match existing purple design system.
* White cards on neutral background.
* Compact spacing.
* Avoid excessive blank space.
* No dense table for Diary Timeline.
* Dedicated detail page for full entry.
* Align financial values clearly.
* Use badges for categories.
* Use subtle shadows and rounded corners.
* Keep action menus unobtrusive.

## 28\. Acceptance Criteria

* \[ ] Diary page matches approved timeline layout.
* \[ ] Filters remain compact.
* \[ ] Search works.
* \[ ] Category/date/customer/loan filters work.
* \[ ] Summary values are correct.
* \[ ] Entries group by date.
* \[ ] Entries sort newest first.
* \[ ] Category badges are consistent.
* \[ ] Tags render.
* \[ ] Snapshot preview only appears when snapshot exists.
* \[ ] Snapshot values reconcile with financial services.
* \[ ] Snapshot is immutable.
* \[ ] Compare to Today works.
* \[ ] Promise-to-Pay cards render.
* \[ ] Follow-up cards render.
* \[ ] Extension cards render.
* \[ ] Mini Calendar marks active dates.
* \[ ] Mini Calendar date click filters Timeline.
* \[ ] Category counts are correct.
* \[ ] Quick Summary reconciles with Dashboard.
* \[ ] Edit/Delete work.
* \[ ] Empty/loading/error states work.
* \[ ] Responsive behavior is usable.
* \[ ] Server-side filtering/paging is used.
* \[ ] Angular never supplies authoritative financial snapshot totals.

## 29\. Implementation Priority

### Phase 1


Diary Timeline
Filters
New/Edit/Delete Entry
Categories
Summary Cards
Snapshot Preview
Diary Detail
Compare to Today


### Phase 2


Mini Calendar
Category Sidebar
Quick Summary
Tags
Reminder integration


### Phase 3


Enhanced Promise-to-Pay cards
Enhanced Follow-up cards
Enhanced Extension cards
Mobile refinement


### Phase 4


Optional infinite scroll
Advanced search
Automatic daily snapshots
Historical trend visualizations


The primary objective is to make the Diary a useful **business journal**, not merely a note-taking page. Each entry should preserve enough lending and financial context that the owner can later understand what happened, why it mattered, and how the business changed afterward.

