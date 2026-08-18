import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export interface CalendarSummaryData {
  rangeLabel: string;
  loanDueCount: number;
  extensionCount: number;
  followUpCount: number;
  promiseCount: number;
  /** SUM(Outstanding Balance of loans whose effective due date falls within the visible month) — loan_due + extension_due only, per requirements' recommended definition. */
  totalAmountDue: number;
}

/** Requirements' compact Monthly Summary — one row of small stat tiles, not the oversized cards the requirements doc explicitly says to avoid. Always reflects the calendar month of the toolbar's current period, independent of Month/Week/Day view. */
@Component({
  selector: 'lm-calendar-summary',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './calendar-summary.component.html',
  styleUrls: ['./calendar-summary.component.scss'],
})
export class CalendarSummaryComponent {
  @Input() summary: CalendarSummaryData | null = null;
}
