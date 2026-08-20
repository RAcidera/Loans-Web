/**
 * Formats a Date using its LOCAL calendar-date components (year/month/day)
 * as a plain "yyyy-MM-dd" string — the one correct way to turn a Date
 * picked from a UI control into the date string the backend expects.
 *
 * Never use `date.toISOString().slice(0, 10)` for this. `mat-date-range-picker`
 * (and `new Date()`) produce a Date representing LOCAL midnight of the
 * intended day; `toISOString()` converts that to UTC first, which shifts
 * the calendar day itself whenever the browser's UTC offset is non-zero —
 * e.g. in the Philippines (UTC+8), picking "Aug 13" becomes local midnight
 * Aug 13 00:00 +08:00, which is Aug 12 16:00 UTC, so `.toISOString().slice(0,10)`
 * silently returns "2026-08-12". This was a real, reported bug: filtering
 * the Payments list to "8/13/2026 - 8/13/2026" showed 8/12's records
 * instead. Every date-range filter and "today" default across the app was
 * affected identically (Loans, Transactions, Payments list filters; the
 * Interest Earned Report's and several dialogs' default date). Fixed by
 * reading getFullYear()/getMonth()/getDate() (all local-time accessors)
 * instead of going through UTC at all.
 */
export function toLocalDateString(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** Today's date as a local "yyyy-MM-dd" string — see toLocalDateString's doc comment for why this must never go through toISOString(). */
export function todayLocalDateString(): string {
  return toLocalDateString(new Date());
}

/** The first day of the current local calendar month, as a "yyyy-MM-dd" string. */
export function firstOfMonthLocalDateString(): string {
  const now = new Date();
  return toLocalDateString(new Date(now.getFullYear(), now.getMonth(), 1));
}

/** The current local time as a plain "HH:mm" string — the Diary form's default Entry Time / Reminder Time (requirements §6). */
export function nowLocalTimeString(): string {
  const now = new Date();
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

/**
 * Adds (or subtracts, for a negative count) whole days to a plain
 * "yyyy-MM-dd" business-date string, returning another "yyyy-MM-dd" string.
 * Deliberately does the arithmetic in UTC — a business date has no
 * time-of-day/timezone meaning, so parsing/reformatting it through the
 * browser's LOCAL timezone (as `new Date(dateString)` + `setDate` would)
 * risks the exact off-by-one-day class of bug this module's other helpers
 * already guard against, just via a different code path.
 */
export function addDaysToDateString(dateString: string, days: number): string {
  const [year, month, day] = dateString.split('-').map(Number);
  const utc = new Date(Date.UTC(year, month - 1, day + days));
  return `${utc.getUTCFullYear()}-${String(utc.getUTCMonth() + 1).padStart(2, '0')}-${String(utc.getUTCDate()).padStart(2, '0')}`;
}
