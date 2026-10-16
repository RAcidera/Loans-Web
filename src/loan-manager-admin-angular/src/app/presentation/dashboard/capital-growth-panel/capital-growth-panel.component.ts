import { Component, ElementRef, NgZone, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import {
  CapitalGrowthMonth,
  CapitalGrowthRange,
  MonthlyCapitalGrowth,
} from '../../../domain/entities/capital-growth.entity';
import { GetMonthlyCapitalGrowthUseCase } from '../../../application/use-cases/get-monthly-capital-growth.use-case';
import { ChartTooltipDirective } from '../../shared/chart-tooltip.directive';

const MONTH_SHORT = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const MONTH_LONG = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

/**
 * Chart height in px. Width is measured from the container at runtime so one
 * SVG unit = one CSS pixel — a fixed viewBox would scale the axis text up
 * along with the chart (wider column → bigger labels), unlike the
 * dashboard's other charts whose labels stay at a constant 9–9.5px.
 */
const H = 250;
const MIN_W = 520;

function plotFor(width: number) {
  return { left: 62, right: width - 70, top: 18, bottom: H - 38 };
}

export const GROWTH_EXPLANATION =
  'Adjusted Capital Growth = change in Total Business Position (Cash on Hand + Gross Receivables), ' +
  'plus owner withdrawals, minus owner deposits. Gross Receivables include uncollected interest and ' +
  'extension charges, so this is capital-position growth — not profit, revenue, ROI, or cash flow.';

interface Tick {
  y: number;
  label: string;
}

interface Slot {
  index: number;
  month: CapitalGrowthMonth;
  x: number;
  slotX: number;
  slotWidth: number;
  bar: { x: number; y: number; width: number; height: number; tone: 'up' | 'down' | 'zero' } | null;
  dot: { x: number; y: number } | null;
  monthLabel: string;
  yearLabel: string;
  showLabel: boolean;
  tooltip: string;
  ariaLabel: string;
}

function fmtMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function fmtSignedMoney(n: number): string {
  return n > 0 ? `+${fmtMoney(n)}` : fmtMoney(n);
}

/** Compact axis labels (₱150K, ₱1.6M) so a 7-digit position fits the narrow axis gutter. */
function fmtAxis(n: number): string {
  const sign = n < 0 ? '-' : '';
  const abs = Math.abs(n);
  if (abs >= 1_000_000) return `${sign}₱${+(abs / 1_000_000).toFixed(abs % 1_000_000 === 0 ? 0 : 1)}M`;
  if (abs >= 1_000) return `${sign}₱${+(abs / 1_000).toFixed(abs % 1_000 === 0 ? 0 : 1)}K`;
  return `${sign}₱${abs.toFixed(0)}`;
}

/** "2026-10" → "October 2026" — parsed from the API's business-local key, never via a browser Date (which would shift with the viewer's timezone). */
export function monthKeyLabel(monthKey: string, long = true): string {
  const [year, month] = monthKey.split('-').map(Number);
  return `${(long ? MONTH_LONG : MONTH_SHORT)[month - 1]} ${year}`;
}

/** "2026-10-15" → "Oct 15, 2026", same no-Date-object reasoning as monthKeyLabel. */
function dateLabel(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return `${MONTH_SHORT[month - 1]} ${day}, ${year}`;
}

/** A rounded [min, max] spanning the data and zero, split into `count` even steps. */
function niceScale(min: number, max: number, count = 4): { min: number; max: number; step: number } {
  if (min === max) {
    max = max === 0 ? 1 : max + Math.abs(max) * 0.5;
    min = Math.min(0, min);
  }
  const rough = (max - min) / count;
  const magnitude = Math.pow(10, Math.floor(Math.log10(rough)));
  const step = [1, 2, 2.5, 5, 10].map((m) => m * magnitude).find((s) => s >= rough) ?? 10 * magnitude;
  return { min: Math.floor(min / step) * step, max: Math.ceil(max / step) * step, step };
}

/**
 * Presentation layer — the Dashboard's "Monthly Capital Growth" panel
 * (Monthly_Capital_Growth requirements §2, §7). Self-contained: fetches its
 * own data so a failure here shows an inline error without blanking the
 * rest of the dashboard. Hand-rolled SVG like the dashboard's other charts
 * (no charting dependency): growth bars on the left axis, Total Business
 * Position line on a separate right axis since the two scales differ by
 * orders of magnitude. Months before recorded history render as shaded
 * "no data" slots, never as zero bars. Selection works by click or by
 * keyboard (roving tabindex: Tab into the chart, Left/Right to move).
 */
@Component({
  selector: 'lm-capital-growth-panel',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTooltipModule, ChartTooltipDirective],
  templateUrl: './capital-growth-panel.component.html',
  styleUrls: ['./capital-growth-panel.component.scss'],
})
export class CapitalGrowthPanelComponent implements OnInit, OnDestroy {
  private readonly getMonthlyCapitalGrowth = inject(GetMonthlyCapitalGrowthUseCase);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly zone = inject(NgZone);

  readonly ranges: CapitalGrowthRange[] = [6, 12, 24];
  readonly explanation = GROWTH_EXPLANATION;
  width = 720;
  plot = plotFor(this.width);
  get viewBox(): string {
    return `0 0 ${this.width} ${H}`;
  }

  private resizeObserver: ResizeObserver | null = null;

  /** The chart's scroll container only exists once data has loaded (it's inside *ngIf), so observe it whenever it (re)appears. */
  @ViewChild('chartBox') set chartBox(el: ElementRef<HTMLElement> | undefined) {
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    if (!el || typeof ResizeObserver === 'undefined') return;

    this.resizeObserver = new ResizeObserver((entries) => {
      const measured = Math.max(MIN_W, Math.round(entries[0].contentRect.width));
      if (measured === this.width || !this.data) return;
      this.zone.run(() => {
        this.width = measured;
        this.buildChart(this.data!);
      });
    });
    this.resizeObserver.observe(el.nativeElement);
  }

  range: CapitalGrowthRange = 12;
  loading = true;
  error: string | null = null;
  data: MonthlyCapitalGrowth | null = null;

  slots: Slot[] = [];
  growthTicks: Tick[] = [];
  positionTicks: Tick[] = [];
  zeroY = this.plot.bottom;
  linePoints = '';
  selectedIndex = -1;
  hasAnyHistory = false;
  /** The last slot whose label is drawn, for the "MTD" marker. */
  currentSlot: Slot | null = null;

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  setRange(range: CapitalGrowthRange): void {
    if (this.range === range || this.loading) return;
    this.range = range;
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.error = null;
    this.getMonthlyCapitalGrowth.execute(this.range).subscribe({
      next: (data) => {
        this.data = data;
        this.loading = false;
        this.buildChart(data);
      },
      error: (err) => {
        this.loading = false;
        this.data = null;
        this.error =
          err?.status === 0 ? 'Could not reach the server. Check your connection and try again.'
          : err?.status === 401 || err?.status === 403 ? 'You do not have access to capital growth figures.'
          : 'Capital growth could not be loaded. Please try again.';
      },
    });
  }

  retry(): void {
    this.load();
  }

  // ---- chart geometry ---------------------------------------------------

  private buildChart(data: MonthlyCapitalGrowth): void {
    const PLOT = (this.plot = plotFor(this.width));
    const months = data.series;
    const growths = months.map((m) => m.adjustedGrowth).filter((v): v is number => v !== null);
    const positions = months.map((m) => m.endingPosition).filter((v): v is number => v !== null);
    this.hasAnyHistory = growths.length > 0;

    const g = niceScale(Math.min(0, ...growths), Math.max(0, ...growths));
    const p = niceScale(Math.min(0, ...positions), Math.max(0, ...positions));
    const plotHeight = PLOT.bottom - PLOT.top;
    const yGrowth = (v: number) => PLOT.top + ((g.max - v) / (g.max - g.min)) * plotHeight;
    const yPosition = (v: number) => PLOT.top + ((p.max - v) / (p.max - p.min)) * plotHeight;

    this.growthTicks = this.ticks(g, yGrowth);
    this.positionTicks = this.ticks(p, yPosition);
    this.zeroY = yGrowth(0);

    const slotWidth = (PLOT.right - PLOT.left) / months.length;
    const labelEvery = months.length > 12 ? 2 : 1;

    this.slots = months.map((month, index) => {
      const slotX = PLOT.left + slotWidth * index;
      const x = slotX + slotWidth / 2;
      const barWidth = Math.min(28, slotWidth * 0.55);
      const growth = month.adjustedGrowth;

      let bar: Slot['bar'] = null;
      if (growth !== null) {
        const y = yGrowth(growth);
        const top = Math.min(y, this.zeroY);
        bar = {
          x: x - barWidth / 2,
          y: growth === 0 ? this.zeroY - 1 : top,
          width: barWidth,
          height: growth === 0 ? 2 : Math.max(Math.abs(y - this.zeroY), 1),
          tone: growth > 0 ? 'up' : growth < 0 ? 'down' : 'zero',
        };
      }

      const [year, mm] = month.monthKey.split('-');
      return {
        index,
        month,
        x,
        slotX,
        slotWidth,
        bar,
        dot: month.endingPosition !== null ? { x, y: yPosition(month.endingPosition) } : null,
        monthLabel: MONTH_SHORT[Number(mm) - 1],
        yearLabel: year,
        // Thin out labels on 24 months, but always label the current month.
        showLabel: index % labelEvery === (months.length - 1) % labelEvery,
        tooltip: this.tooltipFor(month),
        ariaLabel: this.ariaLabelFor(month),
      };
    });

    this.linePoints = this.slots
      .filter((s) => s.dot)
      .map((s) => `${s.dot!.x.toFixed(1)},${s.dot!.y.toFixed(1)}`)
      .join(' ');
    this.currentSlot = this.slots.find((s) => s.month.isPartialMonth) ?? null;
    this.selectedIndex = this.defaultSelection(months);
  }

  private ticks(scale: { min: number; max: number; step: number }, y: (v: number) => number): Tick[] {
    const ticks: Tick[] = [];
    for (let v = scale.min; v <= scale.max + scale.step / 2; v += scale.step) {
      ticks.push({ y: y(v), label: fmtAxis(Math.round(v)) });
    }
    return ticks;
  }

  /** Latest completed month by default — it's the most recent figure that isn't still moving. Falls back to the newest month with any history. */
  private defaultSelection(months: CapitalGrowthMonth[]): number {
    for (let i = months.length - 1; i >= 0; i--) if (months[i].status === 'complete') return i;
    for (let i = months.length - 1; i >= 0; i--) if (months[i].status !== 'nohistory') return i;
    return months.length - 1;
  }

  private tooltipFor(m: CapitalGrowthMonth): string {
    const label = monthKeyLabel(m.monthKey, false) + (m.isPartialMonth ? ' (Month to Date)' : '');
    if (m.status === 'nohistory') return `${label} · No recorded history`;
    const parts = [
      label,
      `Adjusted growth ${fmtSignedMoney(m.adjustedGrowth!)}`,
      `Business position ${fmtMoney(m.endingPosition!)}`,
    ];
    if (m.status === 'partialhistory') parts.push('Records begin this month');
    return parts.join(' · ');
  }

  private ariaLabelFor(m: CapitalGrowthMonth): string {
    const label = monthKeyLabel(m.monthKey) + (m.isPartialMonth ? ', month to date' : '');
    if (m.status === 'nohistory') return `${label}: no recorded history`;
    return `${label}: adjusted capital growth ${fmtSignedMoney(m.adjustedGrowth!)}, business position ${fmtMoney(m.endingPosition!)}`;
  }

  // ---- selection --------------------------------------------------------

  get selected(): CapitalGrowthMonth | null {
    return this.data?.series[this.selectedIndex] ?? null;
  }

  select(index: number): void {
    this.selectedIndex = index;
  }

  onSlotKeydown(event: KeyboardEvent, index: number): void {
    const last = this.slots.length - 1;
    let next: number | null = null;
    if (event.key === 'ArrowRight') next = Math.min(index + 1, last);
    else if (event.key === 'ArrowLeft') next = Math.max(index - 1, 0);
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = last;
    else if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.select(index);
      return;
    }
    if (next === null) return;

    event.preventDefault();
    this.select(next);
    // Roving tabindex: move real focus along with the selection.
    setTimeout(() => (this.host.nativeElement.querySelector(`[data-slot="${next}"]`) as SVGGElement | null)?.focus());
  }

  viewCashTransactions(month: CapitalGrowthMonth): void {
    this.router.navigate(['/cash-funds'], { queryParams: { dateFrom: month.startDate, dateTo: month.endCutoffDate } });
  }

  // ---- template helpers -------------------------------------------------

  monthLabel(monthKey: string | null | undefined): string {
    return monthKey ? monthKeyLabel(monthKey) : '—';
  }

  dateLabel(isoDate: string): string {
    return dateLabel(isoDate);
  }

  money(n: number | null | undefined): string {
    return n === null || n === undefined ? '—' : fmtMoney(n);
  }

  signedMoney(n: number | null | undefined): string {
    return n === null || n === undefined ? '—' : fmtSignedMoney(n);
  }

  percent(n: number | null | undefined): string {
    return n === null || n === undefined ? 'N/A' : `${n > 0 ? '+' : ''}${n.toFixed(2)}%`;
  }

  tone(n: number | null | undefined): 'up' | 'down' | 'zero' {
    return !n ? 'zero' : n > 0 ? 'up' : 'down';
  }

  statusLabel(m: CapitalGrowthMonth): string {
    switch (m.status) {
      case 'partialmonth': return 'Month to Date';
      case 'partialhistory': return 'Records begin this month';
      case 'nohistory': return 'No recorded history';
      default: return 'Completed month';
    }
  }

  /** Reconciliation mismatch is surfaced alongside the server's own warnings, never silently "fixed". */
  get reconciliationMessage(): string | null {
    const r = this.data?.reconciliation;
    if (!r || r.matches || r.difference === null) return null;
    return `Month-to-date ending position differs from the dashboard's Total Business Position by ${fmtMoney(Math.abs(r.difference))}.`;
  }
}
