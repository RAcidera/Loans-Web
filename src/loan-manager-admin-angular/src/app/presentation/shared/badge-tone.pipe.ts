import { Pipe, PipeTransform } from '@angular/core';

/** Cambria UI Toolkit `badge-compact` tones (set via `[attr.data-tone]`). */
export type BadgeTone =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'info'
  | 'warning'
  | 'danger'
  | 'dark'
  | 'light';

/**
 * One place that maps the app's status values to toolkit badge tones,
 * keeping the meanings the old `.chip--<status>` colours carried
 * (good = success, caution = warning, bad = danger, closed/inactive =
 * neutral). Values are never changed — only how they're coloured.
 * The keys don't collide across taxonomies, so one map covers them all.
 */
const TONES: Record<string, BadgeTone> = {
  // Loan status
  active: 'success',
  extended: 'warning',
  overdue: 'danger',
  paid: 'light',
  writtenoff: 'dark',
  // Loan classification
  normal: 'success',
  watchlist: 'warning',
  badloan: 'danger',
  // Customer / user status
  inactive: 'light',
  // Promise-to-pay status
  pending: 'primary',
  rescheduled: 'warning',
  kept: 'success',
  missed: 'danger',
  cancelled: 'light',
  // Cash ledger direction
  in: 'success',
  out: 'danger',
  // Capital growth month status (dashboard)
  complete: 'light',
  partialmonth: 'primary',
  partialhistory: 'warning',
  nohistory: 'light',
};

/**
 * Usage (grid status column — toolkit default, compact soft ALL CAPS):
 *   <span class="badge-compact soft upper" [attr.data-tone]="loan.status | badgeTone">…</span>
 * Detail pages use `titlecase` instead of `upper`.
 */
@Pipe({ name: 'badgeTone', standalone: true })
export class BadgeTonePipe implements PipeTransform {
  transform(value: string | null | undefined): BadgeTone {
    return (value && TONES[value.toLowerCase()]) || 'light';
  }
}
