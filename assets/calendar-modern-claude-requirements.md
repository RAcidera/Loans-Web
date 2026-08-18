# Calendar Module – Claude Code Implementation Requirements

## Goal

Implement the approved compact modern lending calendar. Maximize usable calendar space and make loan-related events easy to scan.

## Layout

Use:

1. Page title
2. Single compact toolbar
3. Event filter chips
4. Compact monthly summary
5. Large calendar grid
6. Small legend

Do not use separate oversized cards for navigation or checkboxes.

## Toolbar

Left:

* Today
* Previous
* Next
* Current period title

Right:

* Month
* Week
* Day
* 

  * New

\+ New dropdown:

* Diary Entry
* Reminder / Follow-up
* Promise to Pay

## Filter Chips

Compact toggles:

* Diary Entries
* Loan Due Dates
* Extension Due Dates
* Follow-up Reminders
* Promise to Pay

All enabled by default.

## Monthly Summary

Show:

* Loan Due count
* Extension count
* Follow-up count
* Promise-to-Pay count
* Total Amount Due

Recommended definition:

Total Amount Due =
SUM(Outstanding Balance of loans whose effective due date
falls within the visible month)



## Views

Support:

* Month
* Week
* Day

Default: Month.

Previous/Next must navigate according to selected view.
Today returns to current date while retaining selected view.

## Month Grid

* 7 columns, Sunday–Saturday.
* Adjacent-month dates visible but muted.
* Today highlighted subtly.
* Desktop day-cell minimum height: 110–125px.
* Keep padding compact.

## Event Cards

Use compact structured cards.

Example:
Loan Due
LOA00001 · Maria Santos
₱3,150.00


Do not use long single-line descriptions.

Use:

* subtle tinted background
* 3px colored left border
* event type label
* max 2 detail lines where possible
* ellipsis for overflow

Colors:

* Loan Due: blue
* Extension Due: orange
* Follow-up: green
* Promise to Pay: teal
* Diary: purple

Keep event colors centralized/configurable.

## Event Overflow

Show no more than 2–3 events in a month cell.

If more:

+3 more


Click should open day details, drawer, or Day view.

Do not expand the row significantly.



## Event Sources

### Diary

Use Diary Entry date/time.

### Loan Due

Use effective/current due date.

Card:

Loan Due
Loan Number · Customer
Outstanding Balance


### Extension Due

Use applicable extension due date.



### Follow-up

Use reminder date/time.



### Promise to Pay

Use PromiseDate and optional PromiseTime.



## Extension Date Logic

Avoid duplicate due events.

Recommended:

* Loan Due uses the current effective due date.
* If extension changes due date, original due date should no longer be shown as active.
* Extension-specific event should only be shown if the business intentionally wants a distinct extension milestone.



## Event Click

* Loan Due → Loan Detail
* Extension Due → Loan Detail / Extensions
* Diary → Diary Detail
* Follow-up → Related Diary/Customer
* Promise to Pay → Promise Detail
* 

## Advanced Filters

Keep advanced filters behind a Filters button.

Optional fields:

* Customer
* Loan
* Loan Status
* Classification
* Borrower Type
* Amount Range

## Responsive Behavior

Desktop:

* Full 7-column month grid.

Tablet:

* Keep 7 columns when practical.
* Reduce padding and event text slightly.

Mobile:

* Prefer Week/Day view or horizontal month scrolling.
* Do not compress cards until unreadable.

Angular Components
calendar-page
calendar-toolbar
calendar-filter-chips
calendar-summary
calendar-month-view
calendar-week-view
calendar-day-view
calendar-day-cell
calendar-event-card
calendar-more-events
calendar-event-detail


## Accessibility

* Do not rely on color alone.
* Always include event type text.
* Event cards keyboard-accessible.
* Buttons have accessible labels/tooltips.
* Maintain sufficient contrast.

## Acceptance Criteria

* \[ ] Compact toolbar implemented.
* \[ ] No oversized navigation/filter panels.
* \[ ] Filter chips implemented.
* \[ ] Month/Week/Day views work.
* \[ ] Today/Previous/Next work correctly.
* \[ ] Today is highlighted.
* \[ ] Loan Due events use effective due date.
* \[ ] Diary, Follow-up, Promise-to-Pay events render.
* \[ ] Extension events follow defined date logic.
* \[ ] Event cards are compact and consistent.
* \[ ] +N more appears when day is crowded.
* \[ ] Clicking events opens correct records.
* \[ ] Summary values reconcile with database.
* \[ ] Total Amount Due uses the defined formula.
* \[ ] Filters update without full page reload.
* \[ ] No duplicate due events.
* \[ ] Responsive behavior is usable.

## Implementation Priority

### Phase 1

* Month View
* Toolbar
* Filter chips
* Loan Due
* Diary
* Follow-up
* Promise to Pay
* Monthly summary

### Phase 2

* Extension due rules
* +N More
* Advanced filters
* New dropdown

### Phase 3

* Week View
* Day View
* Mobile optimization

