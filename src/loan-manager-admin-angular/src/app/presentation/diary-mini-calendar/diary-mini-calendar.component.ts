import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import { SearchDiaryEntriesUseCase } from '../../application/use-cases/search-diary-entries.use-case';
import { toLocalDateString } from '../shared/date-utils';

interface MiniDayCell {
  date: Date;
  dateKey: string;
  dayNumber: number;
  inCurrentMonth: boolean;
  isToday: boolean;
  hasEntry: boolean;
  hasReminder: boolean;
}

const WEEKDAY_LABELS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

/**
 * Requirements diary-modern §20's Mini Calendar — its own always-unfiltered
 * view of "which dates in this month have activity" (independent of the
 * timeline's current search/category/customer/loan filters), so switching
 * months here never depends on, or resets, whatever the main filter bar is
 * doing. Clicking a date emits dateSelected for the page to apply as a
 * timeline date filter.
 */
@Component({
  selector: 'lm-diary-mini-calendar',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './diary-mini-calendar.component.html',
  styleUrls: ['./diary-mini-calendar.component.scss'],
})
export class DiaryMiniCalendarComponent implements OnInit {
  @Output() dateSelected = new EventEmitter<string>();

  current = new Date();
  weeks: MiniDayCell[][] = [];
  selectedDate: string | null = null;
  weekdayLabels = WEEKDAY_LABELS;

  constructor(private readonly searchEntries: SearchDiaryEntriesUseCase) {}

  ngOnInit(): void {
    this.load();
  }

  get periodLabel(): string {
    return this.current.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
  }

  prev(): void {
    this.current = new Date(this.current.getFullYear(), this.current.getMonth() - 1, 1);
    this.load();
  }

  next(): void {
    this.current = new Date(this.current.getFullYear(), this.current.getMonth() + 1, 1);
    this.load();
  }

  selectDate(cell: MiniDayCell): void {
    this.selectedDate = this.selectedDate === cell.dateKey ? null : cell.dateKey;
    this.dateSelected.emit(this.selectedDate ?? '');
  }

  private load(): void {
    const firstOfMonth = new Date(this.current.getFullYear(), this.current.getMonth(), 1);
    const lastOfMonth = new Date(this.current.getFullYear(), this.current.getMonth() + 1, 0);

    this.searchEntries.execute({ dateFrom: toLocalDateString(firstOfMonth), dateTo: toLocalDateString(lastOfMonth) }).subscribe((entries) => {
      const datesWithEntries = new Set(entries.map((e) => e.entryDate));
      const datesWithReminders = new Set(entries.filter((e) => e.reminderDate).map((e) => e.reminderDate!));
      this.weeks = this.buildWeeks(firstOfMonth, datesWithEntries, datesWithReminders);
    });
  }

  private buildWeeks(firstOfMonth: Date, datesWithEntries: Set<string>, datesWithReminders: Set<string>): MiniDayCell[][] {
    const gridStart = new Date(firstOfMonth);
    gridStart.setDate(gridStart.getDate() - gridStart.getDay());
    const todayKey = toLocalDateString(new Date());
    const cells: MiniDayCell[] = [];

    for (let i = 0; i < 42; i++) {
      const date = new Date(gridStart);
      date.setDate(date.getDate() + i);
      const key = toLocalDateString(date);

      cells.push({
        date,
        dateKey: key,
        dayNumber: date.getDate(),
        inCurrentMonth: date.getMonth() === this.current.getMonth(),
        isToday: key === todayKey,
        hasEntry: datesWithEntries.has(key),
        hasReminder: datesWithReminders.has(key),
      });
    }

    // Trailing all-outside-month rows are dropped so the mini calendar
    // doesn't always reserve a fixed 6-row height like the full Calendar
    // page's grid does — this is a compact sidebar widget, not a primary view.
    while (cells.length > 35 && cells.slice(-7).every((c) => !c.inCurrentMonth)) cells.splice(-7);

    const weeks: MiniDayCell[][] = [];
    for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
    return weeks;
  }
}
