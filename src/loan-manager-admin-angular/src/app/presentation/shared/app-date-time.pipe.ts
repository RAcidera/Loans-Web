import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform, inject } from '@angular/core';

import { BusinessTimeService } from '../../application/business-time.service';

/**
 * For fields that are real UTC instants (CreatedAt, UploadedAt, OccurredAt,
 * EntryDateTime, SnapshotDateTime — anything the backend serializes with
 * DateTime.ToString("O")) — wraps Angular's DatePipe, supplying the
 * configured Business Time Zone as its timezone argument instead of
 * whatever timezone the browser happens to be set to. Angular's DatePipe
 * has accepted IANA timezone names as its 3rd arg since v9 (backed by
 * Intl.DateTimeFormat), so this needs no manual offset math and is
 * DST-correct for free.
 *
 * Do NOT use this for plain business-date fields (dueDate, paymentDate,
 * etc.) — those have no time-of-day/timezone meaning at all and must use
 * `appDate` instead, which formats in UTC to avoid ANY timezone shifting
 * the calendar day. Using the wrong one of these two pipes either drops a
 * business date to the wrong day, or displays a timestamp in the wrong
 * timezone — see each pipe's own doc comment.
 */
// Impure (not the default) — its output depends on BusinessTimeService's
// businessTimeZoneId signal, not just the `value` argument Angular's pure-pipe
// memoization tracks, so a pure pipe would keep showing the default
// 'Asia/Manila' formatting after an admin changes the setting until some
// unrelated binding forced re-evaluation. The extra per-CD-cycle calls this
// costs are cheap (DatePipe.transform on a handful of visible rows).
@Pipe({ name: 'appDateTime', standalone: true, pure: false })
export class AppDateTimePipe implements PipeTransform {
  private readonly businessTimeService = inject(BusinessTimeService);
  private readonly datePipe = inject(DatePipe);

  transform(value: string | Date | null | undefined, format = 'medium'): string | null {
    if (value === null || value === undefined || value === '') return null;
    return this.datePipe.transform(value, format, this.businessTimeService.businessTimeZoneId());
  }
}
