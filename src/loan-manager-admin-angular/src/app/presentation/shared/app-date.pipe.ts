import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';

/**
 * For fields that are pure business dates with no time-of-day meaning at
 * all (dueDate, startDate, paymentDate, transactionDate, extensionDate,
 * reminderDate, loanDate, periodStart/periodEndInclusive, fromDate/toDate,
 * todayDate, snapshotDate — plain "yyyy-MM-dd" strings from the backend).
 *
 * Angular's DatePipe parses a bare "yyyy-MM-dd" ISO string as UTC midnight,
 * then by default formats it in the BROWSER's local timezone — for any
 * browser west of UTC that silently prints the previous day (e.g. midnight
 * UTC is 7 PM the prior day in US Eastern). Forcing the format timezone to
 * 'UTC' makes the parsed-as-UTC-midnight instant always print back as the
 * same calendar date, regardless of the browser's timezone or the
 * configured Business Time Zone — which is correct, because a business
 * date isn't an instant in the first place and was never meant to be
 * timezone-converted.
 *
 * Do NOT use this for real timestamps (createdAt, occurredAt, etc.) — use
 * `appDateTime` instead, which converts to the configured Business Time
 * Zone rather than pinning to UTC. See that pipe's own doc comment.
 */
@Pipe({ name: 'appDate', standalone: true, pure: true })
export class AppDatePipe implements PipeTransform {
  private readonly datePipe = inject(DatePipe);

  transform(value: string | Date | null | undefined, format = 'mediumDate'): string | null {
    if (value === null || value === undefined || value === '') return null;
    return this.datePipe.transform(value, format, 'UTC');
  }
}
