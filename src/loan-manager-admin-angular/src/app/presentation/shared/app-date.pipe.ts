import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';

/**
 * For fields that are pure business dates with no time-of-day meaning at
 * all (dueDate, startDate, paymentDate, transactionDate, extensionDate,
 * reminderDate, loanDate, periodStart/periodEndInclusive, fromDate/toDate,
 * todayDate, snapshotDate — plain "yyyy-MM-dd" strings from the backend).
 *
 * Deliberately does NOT pass a timezone argument to Angular's DatePipe.
 * That looks backwards — surely a bare date string needs pinning to some
 * fixed zone so it can't shift? — but Angular's DatePipe does not parse a
 * bare "yyyy-MM-dd" the way the native `Date` constructor or `Date.parse`
 * do (which treat date-only ISO strings as UTC midnight per the ECMAScript
 * spec). Angular's internal `toDate()` special-cases date-only strings and
 * builds the `Date` at LOCAL midnight instead (see `common_module.mjs`'s
 * `createDate()` — a deliberate workaround for old browsers, e.g. IE9,
 * mis-parsing "2015-01-01"). So by the time `formatDate` runs, the instant
 * already IS the browser's local midnight for that exact calendar day.
 * Forcing the format timezone to 'UTC' at that point re-reads that local
 * instant as if it were UTC, shifting it backward a full day on any
 * machine whose OS timezone is east of UTC (Manila/UTC+8 included) — the
 * exact "grid shows a day behind the database" bug this fixed. Passing no
 * timezone leaves DatePipe reading the Date's own local Y/M/D components,
 * which already equal the source string's Y/M/D — always correct,
 * regardless of the browser's or the app's configured Business Time Zone.
 *
 * Do NOT use this for real timestamps (createdAt, occurredAt, etc.) — use
 * `appDateTime` instead, which converts to the configured Business Time
 * Zone. See that pipe's own doc comment.
 */
@Pipe({ name: 'appDate', standalone: true, pure: true })
export class AppDatePipe implements PipeTransform {
  private readonly datePipe = inject(DatePipe);

  transform(value: string | Date | null | undefined, format = 'mediumDate'): string | null {
    if (value === null || value === undefined || value === '') return null;
    return this.datePipe.transform(value, format);
  }
}
