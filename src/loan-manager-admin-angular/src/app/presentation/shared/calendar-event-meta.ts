import { CalendarEventType } from '../../domain/entities/calendar-event.entity';

/**
 * Requirements' fixed per-type legend/chip metadata — deliberately
 * centralized here rather than duplicated across calendar-filter-chips,
 * calendar-summary, calendar-event-card, calendar-event-detail and the page
 * legend.
 *
 * Colours are the app's ORIGINAL per-type legend (same hex values the
 * backend's GetCalendarEventsQuery sends): Loan Due = blue, Extension Due =
 * orange, Follow-up = green, Promise to Pay = teal, Diary = purple (diary
 * events normally use their own category colour; purple is the fallback).
 * Kept by user decision instead of remapping types to toolkit tones.
 *
 * Derived values keep them readable in light AND dark mode:
 *   color — the solid accent (left stripe, legend dot, chip icon)
 *   tint  — translucent card/badge background (14% over the surface)
 *   text  — readable ink: the hue mixed 60/40 toward the theme's text colour
 *           (darker on light surfaces, lighter on dark ones)
 */
export interface CalendarEventMeta {
  label: string;
  icon: string;
  color: string;
  text: string;
  tint: string;
}

const hue = (hex: string): Pick<CalendarEventMeta, 'color' | 'text' | 'tint'> => ({
  color: hex,
  text: `color-mix(in srgb, ${hex} 60%, var(--text))`,
  tint: `color-mix(in srgb, ${hex} 14%, transparent)`,
});

export const CALENDAR_EVENT_META: Record<CalendarEventType, CalendarEventMeta> = {
  diary: { label: 'Diary Entries', icon: 'edit_note', ...hue('#7C3AED') },
  loan_due: { label: 'Loan Due Dates', icon: 'event', ...hue('#2563EB') },
  extension_due: { label: 'Extension Due Dates', icon: 'update', ...hue('#EA580C') },
  reminder: { label: 'Follow-up Reminders', icon: 'notifications', ...hue('#16A34A') },
  promise: { label: 'Promise to Pay', icon: 'handshake', ...hue('#0D9488') },
};

export const CALENDAR_EVENT_TYPES: CalendarEventType[] = ['diary', 'loan_due', 'extension_due', 'reminder', 'promise'];
