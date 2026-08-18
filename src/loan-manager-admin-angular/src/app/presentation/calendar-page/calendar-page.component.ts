import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';

import { CalendarEvent, CalendarEventType } from '../../domain/entities/calendar-event.entity';
import { Customer } from '../../domain/entities/customer.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { GetCalendarEventsUseCase } from '../../application/use-cases/get-calendar-events.use-case';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';
import { toLocalDateString } from '../shared/date-utils';
import { CalendarDayCellData } from '../shared/calendar-day-cell.model';
import { CALENDAR_EVENT_TYPES } from '../shared/calendar-event-meta';

import { CalendarToolbarComponent, CalendarViewMode } from '../calendar-toolbar/calendar-toolbar.component';
import { CalendarFilterChipsComponent } from '../calendar-filter-chips/calendar-filter-chips.component';
import { CalendarSummaryComponent, CalendarSummaryData } from '../calendar-summary/calendar-summary.component';
import { CalendarMonthViewComponent } from '../calendar-month-view/calendar-month-view.component';
import { CalendarWeekViewComponent } from '../calendar-week-view/calendar-week-view.component';
import { CalendarDayViewComponent } from '../calendar-day-view/calendar-day-view.component';
import { CalendarEventDetailComponent } from '../calendar-event-detail/calendar-event-detail.component';
import {
  CalendarAdvancedFiltersDialogComponent,
  CalendarAdvancedFilters,
} from '../calendar-advanced-filters-dialog/calendar-advanced-filters-dialog.component';
import { DiaryFormDialogComponent } from '../diary-form-dialog/diary-form-dialog.component';
import { PromiseFormDialogComponent } from '../promise-form-dialog/promise-form-dialog.component';

/** Cells cap visible events and collapse the rest to "+N more" (requirements' Event Overflow) — Month cells show fewer than Week cells since they're smaller. */
const MAX_VISIBLE_MONTH = 3;
const MAX_VISIBLE_WEEK = 8;

/**
 * Presentation layer — requirements' Calendar module, rebuilt as a
 * compact-toolbar / filter-chips / monthly-summary / large-grid / legend
 * layout (see calendar-modern-claude-requirements.md), composed from the
 * dedicated calendar-* child components rather than one monolithic
 * template. A custom Month/Week/Day grid rather than a calendar npm
 * package — no calendar library exists in this app, and a custom grid
 * gives exact control over the "+N more" overflow and structured event
 * cards the requirements specify precisely.
 */
@Component({
  selector: 'lm-calendar-page',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatDialogModule,
    CalendarToolbarComponent,
    CalendarFilterChipsComponent,
    CalendarSummaryComponent,
    CalendarMonthViewComponent,
    CalendarWeekViewComponent,
    CalendarDayViewComponent,
  ],
  templateUrl: './calendar-page.component.html',
  styleUrls: ['./calendar-page.component.scss'],
})
export class CalendarPageComponent implements OnInit {
  viewMode: CalendarViewMode = 'month';
  current = new Date();
  loading = true;

  enabledTypes = new Set<CalendarEventType>(CALENDAR_EVENT_TYPES);
  advancedFilters: CalendarAdvancedFilters = {};

  monthWeeks: CalendarDayCellData[][] = [];
  weekDays: CalendarDayCellData[] = [];
  dayEvents: CalendarEvent[] = [];
  summary: CalendarSummaryData | null = null;

  private allEvents: CalendarEvent[] = [];
  private monthEvents: CalendarEvent[] = [];
  private loadedMonthKey = '';
  private customers: Customer[] = [];
  private loans: Loan[] = [];
  private loansById = new Map<string, Loan>();
  private customersById = new Map<string, Customer>();

  constructor(
    private readonly getEvents: GetCalendarEventsUseCase,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly getLoans: GetLoansUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({ customers: this.getCustomers.execute(), loans: this.getLoans.execute() }).subscribe(({ customers, loans }) => {
      this.customers = customers;
      this.loans = loans;
      this.customersById = new Map(customers.map((c) => [c.customerId, c]));
      this.loansById = new Map(loans.map((l) => [l.loanId, l]));
      this.render();
    });
    this.load();
  }

  get periodLabel(): string {
    if (this.viewMode === 'day') {
      return this.current.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
    }
    if (this.viewMode === 'week') {
      const { start, end } = this.weekRange(this.current);
      const sameMonth = start.getMonth() === end.getMonth();
      const startLabel = start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
      // A {day:'numeric', year:'numeric'} skeleton (no month) renders garbled
      // in some Chromium/locale combinations — e.g. "2026 (day: 22)" instead
      // of "22, 2026" — so the same-month case is built manually rather than
      // through Intl.
      const endLabel = sameMonth ? `${end.getDate()}, ${end.getFullYear()}` : end.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
      return `${startLabel} – ${endLabel}`;
    }
    return this.current.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
  }

  get advancedFiltersActive(): boolean {
    const f = this.advancedFilters;
    return !!(f.customerId || f.loanId || f.loanStatuses?.length || f.classifications?.length || f.borrowerType || f.amountMin != null || f.amountMax != null);
  }

  setView(mode: CalendarViewMode): void {
    if (mode === this.viewMode) return;
    this.viewMode = mode;
    this.load();
  }

  goToday(): void {
    this.current = new Date();
    this.load();
  }

  prev(): void {
    this.shift(-1);
  }

  next(): void {
    this.shift(1);
  }

  private shift(direction: number): void {
    const d = new Date(this.current);
    if (this.viewMode === 'month') d.setMonth(d.getMonth() + direction);
    else if (this.viewMode === 'week') d.setDate(d.getDate() + direction * 7);
    else d.setDate(d.getDate() + direction);
    this.current = d;
    this.load();
  }

  toggleType(type: CalendarEventType): void {
    if (this.enabledTypes.has(type)) this.enabledTypes.delete(type);
    else this.enabledTypes.add(type);
    this.render();
  }

  openAdvancedFilters(): void {
    this.dialog
      .open(CalendarAdvancedFiltersDialogComponent, {
        width: '460px',
        maxWidth: '95vw',
        data: { customers: this.customers, loans: this.loans, current: this.advancedFilters },
      })
      .afterClosed()
      .subscribe((result: CalendarAdvancedFilters | undefined) => {
        if (result) {
          this.advancedFilters = result;
          this.render();
        }
      });
  }

  private range(): { from: Date; to: Date } {
    if (this.viewMode === 'day') return { from: this.current, to: this.current };
    if (this.viewMode === 'week') {
      const { start, end } = this.weekRange(this.current);
      return { from: start, to: end };
    }

    // Month: a fixed 6-row (42-cell) grid starting the Sunday on/before the
    // 1st — a constant row count avoids the grid's height jumping between
    // 5 and 6 rows as the user navigates month to month.
    const firstOfMonth = new Date(this.current.getFullYear(), this.current.getMonth(), 1);
    const gridStart = new Date(firstOfMonth);
    gridStart.setDate(gridStart.getDate() - gridStart.getDay());
    const gridEnd = new Date(gridStart);
    gridEnd.setDate(gridEnd.getDate() + 41);
    return { from: gridStart, to: gridEnd };
  }

  private weekRange(date: Date): { start: Date; end: Date } {
    const start = new Date(date);
    start.setDate(start.getDate() - start.getDay());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    return { start, end };
  }

  /** The Monthly Summary always reflects the calendar month of `current`, independent of Month/Week/Day view — a Week/Day view straddling a month boundary still shows that period's home month here. */
  private monthRange(): { from: Date; to: Date } {
    const from = new Date(this.current.getFullYear(), this.current.getMonth(), 1);
    const to = new Date(this.current.getFullYear(), this.current.getMonth() + 1, 0);
    return { from, to };
  }

  private load(): void {
    this.loading = true;
    const { from, to } = this.range();
    this.getEvents.execute(toLocalDateString(from), toLocalDateString(to)).subscribe((events) => {
      this.allEvents = events;
      this.render();
      this.loading = false;
    });

    const monthKey = `${this.current.getFullYear()}-${this.current.getMonth()}`;
    if (monthKey !== this.loadedMonthKey) {
      this.loadedMonthKey = monthKey;
      const { from: monthFrom, to: monthTo } = this.monthRange();
      this.getEvents.execute(toLocalDateString(monthFrom), toLocalDateString(monthTo)).subscribe((events) => {
        this.monthEvents = events;
        this.renderSummary();
      });
    } else {
      this.renderSummary();
    }
  }

  private matchesFilters(event: CalendarEvent): boolean {
    if (!this.enabledTypes.has(event.type)) return false;
    const f = this.advancedFilters;

    if (f.customerId && event.customerId !== f.customerId) return false;
    if (f.loanId && event.loanId !== f.loanId) return false;

    if (f.loanStatuses?.length || f.classifications?.length || f.amountMin != null || f.amountMax != null) {
      const loan = event.loanId ? this.loansById.get(event.loanId) : undefined;
      if (f.loanStatuses?.length && (!loan || !f.loanStatuses.includes(loan.status))) return false;
      if (f.classifications?.length && (!loan || !f.classifications.includes(loan.classification))) return false;
      if (f.amountMin != null && (event.amount == null || event.amount < f.amountMin)) return false;
      if (f.amountMax != null && (event.amount == null || event.amount > f.amountMax)) return false;
    }

    if (f.borrowerType) {
      const customer = event.customerId ? this.customersById.get(event.customerId) : undefined;
      if (!customer || customer.borrowerType !== f.borrowerType) return false;
    }

    return true;
  }

  private render(): void {
    const filtered = this.allEvents.filter((e) => this.matchesFilters(e));
    const byDate = new Map<string, CalendarEvent[]>();
    for (const e of filtered) {
      const list = byDate.get(e.date) ?? [];
      list.push(e);
      byDate.set(e.date, list);
    }
    for (const list of byDate.values()) list.sort((a, b) => (a.time ?? '').localeCompare(b.time ?? ''));

    if (this.viewMode === 'month') {
      this.monthWeeks = this.buildWeeks(byDate, MAX_VISIBLE_MONTH);
    } else if (this.viewMode === 'week') {
      this.weekDays = this.buildWeeks(byDate, MAX_VISIBLE_WEEK)[0] ?? [];
    } else {
      this.dayEvents = byDate.get(toLocalDateString(this.current)) ?? [];
    }

    this.renderSummary();
  }

  private renderSummary(): void {
    const filtered = this.monthEvents.filter((e) => this.matchesFilters(e));
    const { from, to } = this.monthRange();
    const rangeLabel = `${from.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – ${to.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}`;

    let loanDueCount = 0;
    let extensionCount = 0;
    let followUpCount = 0;
    let promiseCount = 0;
    let totalAmountDue = 0;

    for (const e of filtered) {
      if (e.type === 'loan_due') {
        loanDueCount++;
        totalAmountDue += e.amount ?? 0;
      } else if (e.type === 'extension_due') {
        extensionCount++;
        totalAmountDue += e.amount ?? 0;
      } else if (e.type === 'reminder') {
        followUpCount++;
      } else if (e.type === 'promise') {
        promiseCount++;
      }
    }

    this.summary = { rangeLabel, loanDueCount, extensionCount, followUpCount, promiseCount, totalAmountDue };
  }

  private buildWeeks(byDate: Map<string, CalendarEvent[]>, maxVisible: number): CalendarDayCellData[][] {
    const { from } = this.range();
    const totalDays = this.viewMode === 'week' ? 7 : 42;
    const todayKey = toLocalDateString(new Date());
    const cells: CalendarDayCellData[] = [];

    for (let i = 0; i < totalDays; i++) {
      const date = new Date(from);
      date.setDate(date.getDate() + i);
      const key = toLocalDateString(date);
      const dayEvents = byDate.get(key) ?? [];

      cells.push({
        date,
        dateKey: key,
        dayNumber: date.getDate(),
        inCurrentMonth: date.getMonth() === this.current.getMonth(),
        isToday: key === todayKey,
        visibleEvents: dayEvents.slice(0, maxVisible),
        overflowCount: Math.max(0, dayEvents.length - maxVisible),
      });
    }

    const weeks: CalendarDayCellData[][] = [];
    for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
    return weeks;
  }

  /** "+N more" / clicking a day number — requirements' overflow handling drills into the Day view for that date rather than expanding the cell. */
  openDay(cell: CalendarDayCellData): void {
    this.current = cell.date;
    this.viewMode = 'day';
    this.load();
  }

  /** Requirements' Event Click table — a quick-preview dialog first, then navigation to the linked record only if the user confirms via "View details". */
  openEvent(event: CalendarEvent): void {
    this.dialog
      .open(CalendarEventDetailComponent, { width: '420px', maxWidth: '95vw', data: event })
      .afterClosed()
      .subscribe((viewRequested: boolean | undefined) => {
        if (!viewRequested || !event.linkedEntityId) return;
        if (event.linkedEntityType === 'diary') this.router.navigate(['/diary', event.linkedEntityId]);
        else if (event.linkedEntityType === 'loan') this.router.navigate(['/loans', event.linkedEntityId]);
      });
  }

  openNewDiaryEntry(): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }

  openNewReminder(): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { presetReminder: true } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }

  openNewPromise(): void {
    this.dialog
      .open(PromiseFormDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }
}
