// Domain layer — mirrors CalendarEventDto from the backend's CalendarController.

/** Requirements §19's five togglable Calendar event sources. */
export type CalendarEventType = 'diary' | 'reminder' | 'loan_due' | 'extension_due' | 'promise';

/** One shape for every event source — Color always comes from the server (a diary entry's category color, or a fixed color for the four system types), never hardcoded here. */
export interface CalendarEvent {
  id: string;
  type: CalendarEventType;
  /** The event-type label shown as the compact card's bold heading, e.g. "Loan Due". */
  title: string;
  date: string; // yyyy-MM-dd
  time?: string; // HH:mm, absent for all-day events (loan/extension due dates)
  color: string;
  linkedEntityType?: 'diary' | 'loan' | 'customer' | 'promise';
  linkedEntityId?: string;
  /** Card detail line 1 — e.g. "LOA00001 · Maria Santos" for a loan/extension due, or a customer name. */
  subtitle?: string;
  /** Card detail line 2 for events with no currency amount — e.g. a follow-up's diary title. */
  detailText?: string;
  /** Outstanding balance (loan_due/extension_due) or promised amount (promise) — absent for diary/reminder. */
  amount?: number;
  /** Resolved regardless of event source — backs the Advanced Filters' Customer field. */
  customerId?: string;
  /** Resolved regardless of event source (absent when the event has no loan link) — backs the Advanced Filters' Loan/Loan Status/Classification fields. */
  loanId?: string;
}
