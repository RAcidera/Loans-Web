import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { forkJoin } from 'rxjs';

import { Loan, LoanStatus } from '../../domain/entities/loan.entity';
import { PaymentWithCustomer } from '../../domain/entities/payment.entity';
import { MonthlyCollection } from '../../domain/entities/dashboard-summary.entity';
import { GetCashSummaryUseCase } from '../../application/use-cases/get-cash-summary.use-case';
import { GetRecentPaymentsUseCase } from '../../application/use-cases/get-recent-payments.use-case';
import { GetDashboardSummaryUseCase } from '../../application/use-cases/get-dashboard-summary.use-case';
import { ChartTooltipDirective } from '../shared/chart-tooltip.directive';
import { AppDatePipe } from '../shared/app-date.pipe';

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

/** Below-threshold-turns-red rule for Cash on Hand — separate from the sign-based trend rule below, since this reflects an absolute reserve level, not a period-over-period change. */
const CASH_ON_HAND_MIN_HEALTHY = 10_000;

interface TrendKpi {
  label: string;
  value: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn' | 'danger';
  changePercent: number | null;
  /** Overrides the value text color for KPIs with a status-threshold rule (Cash on Hand, Bad Loan Receivables) instead of the default neutral text color. */
  valueColor?: string;
  /** Present when the card should be clickable — routes to the page that explains the number. */
  onClick?: () => void;
}

interface ChartDot {
  x: number;
  y: number;
  tooltip: string;
}

interface BarGroup {
  month: string;
  thisYearHeight: number;
  lastYearHeight: number;
  thisYearTooltip: string;
  lastYearTooltip: string;
}

interface DonutSegment {
  label: string;
  value: number;
  pct: number;
  color: string;
  dashArray: string;
  dashOffset: number;
}

function fmtMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function fmtCompactMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
}

/**
 * Presentation layer — SRS dashboard wireframe. Composes three use cases
 * (cash summary, recent payments, dashboard summary) into the KPI row
 * (Cash on Hand included as a plain KPI, not a standalone chart card),
 * three charts (hand-rolled inline SVG — no charting library dependency
 * for three fixed-shape charts), and the Recent Loans/Recent Payments
 * feeds. Layout/palette follow the "dashboard-modern.html" reference
 * using this app's existing design tokens (--lm-primary/success/danger/
 * warning etc.) rather than a new palette. Every trend badge uses one
 * plain rule regardless of what the metric means: positive → green + up
 * arrow, negative → red + down arrow, zero → neutral dash. Two KPIs (Cash
 * on Hand, Bad Loan Receivables) additionally override their value text
 * color against a fixed health threshold rather than the trend — a low
 * cash reserve or any bad-loan balance is a standing problem worth
 * flagging regardless of which direction it moved this month. Every KPI
 * card is clickable, routing to the page that explains the number
 * (Quick Actions was removed in favor of this — one click from the
 * number itself, rather than a separate button grid).
 */
@Component({
  selector: 'lm-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatIconModule, MatButtonModule, ChartTooltipDirective, AppDatePipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  kpis: TrendKpi[] = [];
  recentPayments: PaymentWithCustomer[] = [];
  recentLoans: Loan[] = [];
  statusLabel = STATUS_LABEL;
  recentLoanColumns = ['id', 'customer', 'principal', 'dueDate', 'balance', 'status', 'actions'];

  monthlyCollections: MonthlyCollection[] = [];
  barChartMax = 0;
  barGroups: BarGroup[] = [];

  last7DaysLinePoints = '';
  last7DaysAreaPath = '';
  last7DaysDots: ChartDot[] = [];
  last7DaysLabels: string[] = [];
  last7DaysMax = 0;

  donutSegments: DonutSegment[] = [];
  donutTotal = 0;

  constructor(
    private readonly getCashSummary: GetCashSummaryUseCase,
    private readonly getRecentPayments: GetRecentPaymentsUseCase,
    private readonly getDashboardSummary: GetDashboardSummaryUseCase,
    readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({
      cash: this.getCashSummary.execute(),
      summary: this.getDashboardSummary.execute(),
    }).subscribe(({ cash, summary }) => {
      this.recentLoans = summary.recentLoans;
      this.monthlyCollections = summary.monthlyCollections;
      this.buildBarChart(summary.monthlyCollections);
      this.buildLineChart(summary.last7DaysCollections);
      this.buildDonutChart(summary.receivablesBreakdown);

      const goToLoans = (status?: string, classification?: string) => () =>
        this.router.navigate(['/loans'], { queryParams: { ...(status ? { status } : {}), ...(classification ? { classification } : {}) } });
      const cashHealthy = cash.cashOnHand >= CASH_ON_HAND_MIN_HEALTHY;
      const hasBadLoans = summary.badLoanReceivables > 0;

      this.kpis = [
        { label: 'Gross Receivables', value: fmtMoney(summary.grossReceivables), icon: 'account_balance_wallet', tone: 'neutral', changePercent: summary.grossReceivablesChangePercent, onClick: () => this.router.navigate(['/reports']) },
        { label: 'Collectible Receivables', value: fmtMoney(summary.collectibleReceivables), icon: 'savings', tone: 'good', changePercent: summary.collectibleReceivablesChangePercent, onClick: () => this.router.navigate(['/reports']) },
        { label: 'Bad Loan Receivables', value: fmtMoney(summary.badLoanReceivables), icon: 'warning_amber', tone: hasBadLoans ? 'danger' : 'good', changePercent: summary.badLoanReceivablesChangePercent, valueColor: hasBadLoans ? 'var(--lm-danger)' : 'var(--lm-success)', onClick: goToLoans(undefined, 'badloan') },
        { label: 'Cash on Hand', value: fmtCompactMoney(cash.cashOnHand), icon: 'payments', tone: cashHealthy ? 'good' : 'danger', changePercent: cash.cashOnHandChangePercent, valueColor: cashHealthy ? 'var(--lm-success)' : 'var(--lm-danger)', onClick: () => this.router.navigate(['/cash-funds']) },
        { label: 'Total Business Position', value: fmtMoney(summary.grossReceivables + cash.cashOnHand), icon: 'account_balance', tone: 'neutral', changePercent: null, onClick: () => this.router.navigate(['/reports']) },
        { label: 'Active Loans', value: `${summary.activeLoansCount}`, icon: 'receipt_long', tone: 'good', changePercent: summary.activeLoansChangePercent, onClick: goToLoans('active') },
        { label: 'Overdue Loans', value: `${summary.overdueLoansCount}`, icon: 'error_outline', tone: summary.overdueLoansCount > 0 ? 'danger' : 'neutral', changePercent: summary.overdueLoansChangePercent, onClick: goToLoans('overdue') },
      ];
    });

    this.getRecentPayments.execute(5).subscribe((payments) => (this.recentPayments = payments));
  }

  private buildBarChart(months: MonthlyCollection[]): void {
    const max = Math.max(1, ...months.map((m) => Math.max(m.thisYear, m.lastYear)));
    this.barChartMax = this.niceMax(max);
    this.barGroups = months.map((m) => ({
      month: m.month,
      thisYearHeight: (m.thisYear / this.barChartMax) * 100,
      lastYearHeight: (m.lastYear / this.barChartMax) * 100,
      thisYearTooltip: `${m.month} 2026: ${fmtMoney(m.thisYear)}`,
      lastYearTooltip: `${m.month} 2025: ${fmtMoney(m.lastYear)}`,
    }));
  }

  private buildLineChart(days: { date: string; amount: number }[]): void {
    const width = 600;
    const height = 200;
    const max = this.niceMax(Math.max(1, ...days.map((d) => d.amount)));
    this.last7DaysMax = max;
    const step = days.length > 1 ? width / (days.length - 1) : 0;

    const points: ChartDot[] = days.map((d, i) => ({
      x: i * step,
      y: height - (d.amount / max) * height,
      tooltip: `${new Date(d.date).toLocaleDateString(undefined, { month: 'short', day: '2-digit' })}: ${fmtMoney(d.amount)}`,
    }));

    this.last7DaysDots = points;
    this.last7DaysLinePoints = points.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    this.last7DaysAreaPath = `M0,${height} L${points.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' L')} L${width},${height} Z`;
    this.last7DaysLabels = days.map((d) => new Date(d.date).toLocaleDateString(undefined, { month: 'short', day: '2-digit' }));
  }

  private buildDonutChart(breakdown: { current: number; overdue: number; badLoan: number; paid: number }): void {
    const total = breakdown.current + breakdown.overdue + breakdown.badLoan + breakdown.paid;
    this.donutTotal = total;
    const radius = 70;
    const circumference = 2 * Math.PI * radius;

    const parts: { label: string; value: number; color: string }[] = [
      { label: 'Current', value: breakdown.current, color: 'var(--lm-success)' },
      { label: 'Overdue', value: breakdown.overdue, color: 'var(--lm-warning)' },
      { label: 'Bad Loan', value: breakdown.badLoan, color: 'var(--lm-danger)' },
      { label: 'Paid', value: breakdown.paid, color: 'var(--lm-text-muted)' },
    ];

    let cumulative = 0;
    this.donutSegments = parts.map((p) => {
      const fraction = total > 0 ? p.value / total : 0;
      const length = fraction * circumference;
      const segment: DonutSegment = {
        label: p.label,
        value: p.value,
        pct: Math.round(fraction * 1000) / 10,
        color: p.color,
        dashArray: `${length} ${circumference - length}`,
        dashOffset: -cumulative,
      };
      cumulative += length;
      return segment;
    });
  }

  private niceMax(value: number): number {
    const magnitude = Math.pow(10, Math.floor(Math.log10(Math.max(1, value))));
    return Math.ceil(value / magnitude) * magnitude;
  }

  fmtCompact(n: number): string {
    return fmtCompactMoney(n);
  }

  fmtMoney(n: number): string {
    return fmtMoney(n);
  }

  /** One plain rule for every trend badge on the page: positive → up/green, negative → down/red, zero → dash/neutral. */
  trendIcon(changePercent: number): string {
    return changePercent > 0 ? 'arrow_upward' : changePercent < 0 ? 'arrow_downward' : 'remove';
  }

  trendClass(changePercent: number): string {
    return changePercent > 0 ? 'good' : changePercent < 0 ? 'bad' : '';
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  openLoanDetails(loan: Loan): void {
    this.router.navigate(['/loans', loan.loanId]);
  }
}
