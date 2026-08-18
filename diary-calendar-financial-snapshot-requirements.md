# Diary, Calendar, Financial Snapshot, and Compare-to-Today Module

## 1. Purpose

Add a **Diary / Journal** module and a **Calendar** module to the lending application.

The main purpose is to allow the owner to record important business events, observations, collection notes, customer-related events, and financial milestones, then return to those entries months later and compare the financial position captured at that time against the latest current figures.

The key feature is:

**Financial Snapshot + Compare to Today**

This turns the Diary into a lightweight business journal backed by actual financial data.

## 2. Main Concepts

### Diary / Journal

The Diary stores chronological business notes and important events.

Examples:

- Strong collection day
- Slow collection week
- Customer promised to pay
- Customer became overdue
- Loan classified as bad loan
- Large payment received
- Large loan released
- Cash position improved
- Receivables increased significantly
- Business observation or reminder

### Financial Snapshot

A Diary entry can optionally capture the business's current financial position at the exact time the entry is saved.

The snapshot must be stored as historical data and must **not** be recalculated later when the Diary entry is reopened.

### Compare to Today

When viewing a Diary entry with a financial snapshot, the system should allow the user to compare the saved historical figures against the latest current business figures.

### Calendar

The Calendar provides Month, Week, and Day views for:

- Diary entries
- Loan due dates
- Extension due dates
- Follow-up reminders
- Promise-to-pay dates

The Diary and Calendar should be integrated rather than implemented as unrelated modules.

## 3. Navigation

Add the following navigation items:

- Diary
- Calendar

Suggested icons:

- Diary: journal / notebook icon
- Calendar: calendar icon

## 4. Diary Entry Data

Create a Diary Entry with the following fields:

```text
Id
EntryDate
EntryTime
Title
CategoryId
Notes
CustomerId nullable
LoanId nullable
ReminderDate nullable
ReminderTime nullable
CreatedBy
CreatedDate
ModifiedBy
ModifiedDate
```

Suggested display field:

```text
EntryDateTime
```

This can be derived from EntryDate and EntryTime or stored as a DateTime.

## 5. Diary Categories

Create configurable Diary categories.

Initial values:

```text
General
Collections
Receivables
Customer
Loan
Bad Loan
Cash
Follow-up
Important Event
Other
```

The category should support:

```text
Id
Name
Icon
DisplayColor
IsActive
SortOrder
```

Do not hard-code category colors throughout the Angular application.

## 6. Create Diary Entry Screen

Suggested layout:

```text
New Diary Entry

Title
[ __________________________________________ ]

Category
[ Collections ▼ ]

Date
[ Aug 15, 2026 ]

Time
[ 5:30 PM ]

Notes
┌──────────────────────────────────────────────┐
│ Collections have improved this week.        │
│ Several overdue customers resumed paying.   │
│                                              │
└──────────────────────────────────────────────┘

Financial Snapshot
☑ Capture current financial snapshot

Link To
Customer    [ Optional customer ▼ ]
Loan        [ Optional loan ▼ ]

Reminder
☐ Add reminder

Reminder Date [ __________ ]
Reminder Time [ __________ ]

                         [ Cancel ] [ Save Entry ]
```

Defaults:

```text
EntryDate = Current Date
EntryTime = Current Time
CaptureFinancialSnapshot = false
```

Allow the user to override Entry Date and Entry Time.

## 7. Financial Snapshot

When:

```text
Capture current financial snapshot = true
```

the server must calculate and save the current financial position.

Do not accept financial snapshot totals calculated by Angular.

All snapshot values must be generated on the server from authoritative database data.

## 8. Financial Snapshot Fields

Create a separate table:

```text
DiaryFinancialSnapshot
```

Suggested fields:

```text
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
```

Optional future fields:

```text
TotalCustomers
ActiveCustomerCount
LoansDueThisWeek
AverageOutstandingBalance
TotalPrincipalOutstanding
InterestReceivable
```

## 9. Snapshot Calculation Rules

Reuse the same calculation services used by the Dashboard and Reports.

Do not duplicate financial formulas in the Diary module.

### Gross Receivables

```text
Gross Receivables =
SUM(
    Principal
    + Interest
    + Extension Charges
    - Payments
)
```

Include bad loans.

Exclude written-off loans.

### Bad Loan Receivables

```text
Bad Loan Receivables =
SUM(
    Outstanding Balance
    WHERE Loan Classification = Bad Loan
)
```

### Collectible Receivables

```text
Collectible Receivables =
Gross Receivables
-
Bad Loan Receivables
```

### Cash on Hand

```text
Cash on Hand =
SUM(Cash In)
-
SUM(Cash Out)
```

### Collections Today

```text
Collections Today =
SUM(Payments received on Snapshot Date)
```

### Collections Month to Date

```text
Collections Month To Date =
SUM(Payments received
    from first day of Snapshot month
    through Snapshot Date)
```

### Loan Releases Today

```text
Loan Releases Today =
SUM(Principal released on Snapshot Date)
```

### Loan Releases Month to Date

```text
Loan Releases Month To Date =
SUM(Principal released
    from first day of Snapshot month
    through Snapshot Date)
```

## 10. Snapshot Must Be Immutable

Once captured, the financial snapshot must remain historical.

Example:

```text
Diary Entry Date:
Aug 15, 2026

Gross Receivables:
₱385,250
```

If the same Diary entry is opened six months later, it must still show:

```text
₱385,250
```

Do not dynamically recalculate it using current data.

Editing the Title, Notes, Category, Customer, Loan, or Reminder must not modify the stored financial snapshot.

If a future requirement allows snapshot regeneration, it must be an explicit action with audit history.

## 11. Diary List / Timeline Page

Do not use a normal dense data grid as the primary Diary view.

Use a chronological timeline layout.

Suggested layout:

```text
Diary / Journal                                  + New Entry

[ Search diary... ] [ Category ▼ ] [ Date Range ] [ Clear ]

TODAY — AUGUST 15, 2026
──────────────────────────────────────────────────────────────

● 5:20 PM    Collections

  Good collection week

  Collections have improved this week.
  Several overdue customers resumed making payments.

  FINANCIAL SNAPSHOT

  Gross Receivables        ₱385,250
  Collectible              ₱341,800
  Bad Loans                 ₱43,450
  Cash on Hand              ₱82,300

  Collections Today          ₱8,450

  [ View Snapshot ]             [ Edit ] [ ⋮ ]
```

Sort by:

```text
EntryDateTime DESC
```

## 12. Diary Search and Filters

Provide:

```text
Search
Category
Date From
Date To
Customer
Loan
Has Financial Snapshot
Has Reminder
```

Search should match:

```text
Title
Notes
Customer Name
Customer Code
Loan Number
```

## 13. Diary Entry Detail Page

Recommended sections:

```text
Entry Header
Title
Category
Entry Date / Time
Linked Customer
Linked Loan
Notes
Financial Snapshot
Compare to Today
Reminder
Audit Information
```

## 14. Financial Snapshot Display

Example:

```text
FINANCIAL SNAPSHOT
August 15, 2026 at 5:20 PM

Gross Receivables         ₱385,250
Collectible Receivables   ₱341,800
Bad Loan Receivables       ₱43,450
Cash on Hand               ₱82,300

Active Loans                    38
Overdue Loans                   11
Bad Loans                        5

Collections Today           ₱8,450
Collections MTD            ₱67,250

Loans Released Today       ₱10,000
Loans Released MTD         ₱85,000
```

## 15. Compare to Today

Add:

```text
[ Compare to Today ]
```

When selected, calculate the current financial figures and compare them against the stored snapshot.

Suggested layout:

```text
Financial Comparison

Snapshot:
Aug 15, 2026

Today:
Feb 15, 2027

                           AUG 15       TODAY       CHANGE       %

Gross Receivables         ₱385,250     ₱421,800    +₱36,550    +9.5%
Collectible Receivables   ₱341,800     ₱389,100    +₱47,300   +13.8%
Bad Loan Receivables       ₱43,450      ₱32,700    -₱10,750   -24.7%
Cash on Hand               ₱82,300     ₱116,500    +₱34,200   +41.6%
Active Loans                    38           42           +4   +10.5%
Overdue Loans                   11            8           -3   -27.3%
Bad Loans                        5            4           -1   -20.0%
```

## 16. Comparison Formula

```text
Change =
Current Value - Snapshot Value
```

```text
Percentage Change =
(Change / Snapshot Value) × 100
```

Handle zero snapshot values safely.

If Snapshot Value = 0, display percentage as `N/A` or `New`.

## 17. Comparison Colors

Use contextual colors.

Positive outcomes:

- Bad Loan Receivables decreased → green
- Overdue Loans decreased → green
- Cash on Hand increased → green

Negative outcomes:

- Bad Loan Receivables increased → red
- Overdue Loans increased → red

Use neutral purple/blue where direction is not inherently good or bad, such as Gross Receivables.

## 18. Calendar Module

Create a Calendar page with:

```text
Month
Week
Day
```

Default:

```text
Month
```

Controls:

```text
Today
Previous
Next
Month | Week | Day
```

## 19. Calendar Event Sources

Display events from:

```text
Diary Entries
Loan Due Dates
Loan Extension Due Dates
Follow-up Reminders
Promise to Pay
```

Allow toggling event types.

## 20. Promise to Pay

Add optional Promise-to-Pay functionality because customer payments are irregular.

Suggested fields:

```text
Id
CustomerId
LoanId
PromiseDate
Amount
Notes
Status
CreatedBy
CreatedDate
ModifiedBy
ModifiedDate
```

Statuses:

```text
Pending
Kept
Missed
Rescheduled
Cancelled
```

## 21. Suggested Backend Services

```text
IDiaryService
DiaryService
```

Responsibilities:

```text
CreateDiaryEntry()
UpdateDiaryEntry()
DeleteDiaryEntry()
GetDiaryEntry()
SearchDiaryEntries()
```

Financial snapshot service:

```text
IFinancialSnapshotService
FinancialSnapshotService
```

Responsibilities:

```text
CaptureCurrentSnapshot()
GetCurrentFinancialPosition()
CompareSnapshotToCurrent()
```

Calendar service:

```text
ICalendarService
CalendarService
```

Responsibilities:

```text
GetEvents(fromDate, toDate)
GetDiaryEvents()
GetLoanDueEvents()
GetExtensionDueEvents()
GetReminderEvents()
GetPromiseToPayEvents()
```

## 22. Suggested API Endpoints

Diary:

```text
GET    /api/diary
GET    /api/diary/{id}
POST   /api/diary
PUT    /api/diary/{id}
DELETE /api/diary/{id}
```

Snapshot:

```text
GET /api/diary/{id}/snapshot
GET /api/diary/{id}/compare-to-today
```

Calendar:

```text
GET /api/calendar/events
```

Promise to Pay:

```text
GET    /api/promises-to-pay
GET    /api/promises-to-pay/{id}
POST   /api/promises-to-pay
PUT    /api/promises-to-pay/{id}
DELETE /api/promises-to-pay/{id}
```

## 23. Suggested Angular Pages / Components

```text
Diary
  diary-list
  diary-detail
  diary-form
  financial-snapshot
  financial-comparison

Calendar
  calendar-page
  calendar-event-detail

PromiseToPay
  promise-form
  promise-detail
```

Reusable components:

```text
financial-metric
comparison-metric
category-badge
calendar-event
```

## 24. Audit Requirements

Record:

```text
Diary Created
Diary Updated
Diary Deleted
Snapshot Captured
Reminder Changed
Linked Customer Changed
Linked Loan Changed
Promise Created
Promise Updated
Promise Kept
Promise Missed
Promise Rescheduled
Promise Cancelled
```

Financial snapshot values must not be modified during normal Diary editing.

## 25. User Experience Requirements

The module should visually match the existing modern application design:

- White cards
- Light neutral page background
- Purple primary actions
- Subtle contextual status colors
- Responsive layout
- Compact spacing
- Clear financial emphasis
- Avoid excessive popups

Diary Detail should preferably use a dedicated page rather than a small modal.

Calendar should remain readable even when many events exist on the same day.

When more than the maximum visible events fit inside a Month cell:

```text
+3 more
```

should appear.

## 26. Primary User Flow

```text
1. User notices something significant in the business.
2. User opens Diary.
3. Clicks New Entry.
4. Writes a note.
5. Selects a category.
6. Checks Capture current financial snapshot.
7. Saves entry.
8. System stores both the Diary note and current financial snapshot.
9. Several months later, user opens the same Diary entry.
10. Historical financial figures remain unchanged.
11. User clicks Compare to Today.
12. System calculates today's current figures.
13. System displays Snapshot vs Today vs Change vs Percentage.
14. User can immediately evaluate how the business has changed.
```

## 27. Acceptance Criteria

- [ ] User can create Diary entries.
- [ ] Diary entries support categories.
- [ ] Diary entries can optionally link to Customer.
- [ ] Diary entries can optionally link to Loan.
- [ ] Diary entries can optionally contain reminders.
- [ ] User can choose to capture a financial snapshot.
- [ ] Financial snapshot values are calculated server-side.
- [ ] Snapshot is stored permanently with the Diary entry.
- [ ] Editing Diary notes does not recalculate the snapshot.
- [ ] User can view historical financial snapshot.
- [ ] User can compare historical snapshot with current financial figures.
- [ ] Difference values are calculated correctly.
- [ ] Percentage values are calculated correctly.
- [ ] Zero-value percentage comparisons are handled safely.
- [ ] Calendar supports Month view.
- [ ] Calendar supports Week view.
- [ ] Calendar supports Day view.
- [ ] Diary entries appear on Calendar.
- [ ] Loan due dates appear on Calendar.
- [ ] Extension due dates appear on Calendar.
- [ ] Reminders appear on Calendar.
- [ ] Promise-to-pay records appear on Calendar.
- [ ] Calendar events navigate to the related record.
- [ ] Snapshot calculations reconcile with Dashboard calculations.
- [ ] Cash on Hand reconciles with Cash Ledger.
- [ ] Receivables reconcile with Loan calculations.
- [ ] Audit history is retained for important changes.

## 28. Implementation Priority

Recommended order:

```text
Phase 1
Diary CRUD
Diary Categories
Financial Snapshot capture
Diary Timeline
Diary Detail
Compare to Today

Phase 2
Calendar Month / Week / Day
Diary Calendar Events
Loan Due Events
Extension Due Events
Diary Reminders

Phase 3
Promise to Pay
Promise status workflow
Calendar integration

Phase 4
Optional automatic Daily Financial Snapshots
Historical analytics and trend charts
```

Highest priority:

**Diary Entry + Financial Snapshot + Compare to Today**
