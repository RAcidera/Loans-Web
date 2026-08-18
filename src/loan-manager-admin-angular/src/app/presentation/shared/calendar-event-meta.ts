import { CalendarEventType } from '../../domain/entities/calendar-event.entity';

/**
 * Requirements' fixed per-type legend/chip metadata — deliberately
 * centralized here rather than duplicated across calendar-filter-chips,
 * calendar-summary, and calendar-event-card. The actual event.color on a
 * given CalendarEvent still comes from the server (see CalendarEventDto's
 * doc comment — a diary entry's own category color, etc.); this base color
 * only backs UI chrome that has no single event to color from, like a
 * filter chip or the legend.
 */
export interface CalendarEventMeta {
  label: string;
  icon: string;
  color: string;
}

export const CALENDAR_EVENT_META: Record<CalendarEventType, CalendarEventMeta> = {
  diary: { label: 'Diary Entries', icon: 'edit_note', color: '#7C3AED' },
  loan_due: { label: 'Loan Due Dates', icon: 'event', color: '#2563EB' },
  extension_due: { label: 'Extension Due Dates', icon: 'update', color: '#EA580C' },
  reminder: { label: 'Follow-up Reminders', icon: 'notifications', color: '#16A34A' },
  promise: { label: 'Promise to Pay', icon: 'handshake', color: '#0D9488' },
};

export const CALENDAR_EVENT_TYPES: CalendarEventType[] = ['diary', 'loan_due', 'extension_due', 'reminder', 'promise'];
