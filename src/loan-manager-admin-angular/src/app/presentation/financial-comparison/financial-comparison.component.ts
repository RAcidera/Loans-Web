import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { FinancialComparison, FinancialComparisonMetric } from '../../domain/entities/diary-entry.entity';
import { AppDatePipe } from '../shared/app-date.pipe';

/** Which direction of change is a "good" outcome for a given metric key — requirements §17. Money-shaped keys render with a peso sign; count-shaped keys render as plain numbers. */
type Direction = 'lowerIsBetter' | 'higherIsBetter' | 'neutral';

const METRIC_DIRECTION: Record<string, Direction> = {
  grossReceivables: 'neutral', // requirements §17's own example of "not inherently good or bad"
  collectibleReceivables: 'neutral',
  badLoanReceivables: 'lowerIsBetter',
  cashOnHand: 'higherIsBetter',
  totalBusinessPosition: 'neutral',
  activeLoanCount: 'neutral',
  overdueLoanCount: 'lowerIsBetter',
  badLoanCount: 'lowerIsBetter',
};

const COUNT_KEYS = new Set(['activeLoanCount', 'overdueLoanCount', 'badLoanCount']);

/**
 * Requirements §15-17 — the Snapshot vs. Today vs. Change vs. % comparison
 * table, with contextual coloring driven by each metric's own direction
 * rule rather than a blanket positive/negative-number rule. Redesigned per
 * design assets/diary-detail-snapshot.png: a card header with an Export
 * action, direction-arrow icons alongside the Change/% cells, and a
 * "Since This Snapshot" summary band beneath the table.
 */
@Component({
  selector: 'lm-financial-comparison',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, AppDatePipe],
  template: `
    <div class="comparison" *ngIf="comparison">
      <div class="comparison__header">
        <div class="comparison__heading">
          <span class="comparison__title">Financial Comparison</span>
          <span class="comparison__subtitle">
            Snapshot vs Today ({{ comparison.todayDate | appDate: 'MMM d, y' }})
          </span>
          <span class="comparison__elapsed">
            <mat-icon>schedule</mat-icon>
            {{ elapsedText() }}
          </span>
        </div>
        <button mat-stroked-button (click)="exportCsv()">
          <mat-icon>download</mat-icon>
          Export
        </button>
      </div>

      <div class="comparison-scroll">
        <table class="comparison__table">
          <thead>
            <tr>
              <th class="comparison__metric-head">Metric</th>
              <th>
                Snapshot
                <span class="comparison__col-date mono">{{ comparison.snapshotDate | appDate: 'MMM d, y' }}</span>
              </th>
              <th>
                Today
                <span class="comparison__col-date mono">{{ comparison.todayDate | appDate: 'MMM d, y' }}</span>
              </th>
              <th class="right">Change</th>
              <th class="right">% Change</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let metric of comparison.metrics">
              <td class="comparison__metric-label">{{ metric.label }}</td>
              <td class="mono">{{ formatValue(metric) }}</td>
              <td class="mono">{{ formatValue(metric, true) }}</td>
              <td class="mono" [ngClass]="colorClass(metric)">
                <span class="comparison__change">
                  <mat-icon *ngIf="metric.change !== 0">{{ changeIcon(metric.change) }}</mat-icon>
                  <span *ngIf="metric.change === 0">—</span>
                  <ng-container *ngIf="metric.change !== 0">{{ formatChange(metric) }}</ng-container>
                </span>
              </td>
              <td class="mono" [ngClass]="colorClass(metric)">{{ formatPercent(metric) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="since-snapshot">
        <div class="since-snapshot__header">
          <mat-icon>trending_up</mat-icon>
          <span>Since This Snapshot</span>
        </div>
        <div class="since-snapshot__grid">
          <div class="since-snapshot__cell" *ngIf="metric('cashOnHand') as m">
            <span class="since-snapshot__label">Cash on Hand</span>
            <span class="since-snapshot__value" [ngClass]="colorClass(m)">
              <mat-icon *ngIf="m.change !== 0">{{ changeIcon(m.change) }}</mat-icon>
              <span class="mono">{{ m.change === 0 ? '—' : formatChange(m) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell">
            <span class="since-snapshot__label">Collections Received</span>
            <span class="since-snapshot__value good">
              <mat-icon *ngIf="comparison.collectionsSinceSnapshot > 0">arrow_upward</mat-icon>
              <span class="mono">{{ formatMoney(comparison.collectionsSinceSnapshot) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell">
            <span class="since-snapshot__label">Loans Released</span>
            <span class="since-snapshot__value neutral">
              <span class="mono">{{ formatMoney(comparison.loanReleasesSinceSnapshot) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell" *ngIf="metric('grossReceivables') as m">
            <span class="since-snapshot__label">Receivables Change</span>
            <span class="since-snapshot__value" [ngClass]="colorClass(m)">
              <mat-icon *ngIf="m.change !== 0">{{ changeIcon(m.change) }}</mat-icon>
              <span class="mono">{{ m.change === 0 ? '—' : formatChange(m) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell">
            <span class="since-snapshot__label">Owner Withdrawals</span>
            <span class="since-snapshot__value neutral">
              <span class="mono">{{ formatMoney(comparison.ownerWithdrawals) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell">
            <span class="since-snapshot__label">Owner Deposits</span>
            <span class="since-snapshot__value neutral">
              <span class="mono">{{ formatMoney(comparison.ownerDeposits) }}</span>
            </span>
          </div>
          <div class="since-snapshot__cell since-snapshot__cell--highlight">
            <span class="since-snapshot__label since-snapshot__label--caps">Adjusted Capital Growth</span>
            <span class="since-snapshot__value since-snapshot__value--large" [ngClass]="signClass(comparison.adjustedCapitalGrowth)">
              <mat-icon *ngIf="comparison.adjustedCapitalGrowth !== 0">{{ changeIcon(comparison.adjustedCapitalGrowth) }}</mat-icon>
              <span class="mono">{{ formatSignedMoney(comparison.adjustedCapitalGrowth) }}</span>
            </span>
            <span class="since-snapshot__subtext">
              <span class="mono">{{ formatSignedPercent(comparison.adjustedCapitalGrowthPercent) }}</span>
              of starting business position
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .comparison__header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--space-3);
        flex-wrap: wrap;
        margin-bottom: var(--space-4);
      }

      .comparison__heading {
        display: flex;
        flex-direction: column;
        gap: 2px;
      }

      .comparison__title {
        font-size: 14px;
        line-height: 20px;
        font-weight: 600;
        color: var(--text);
      }

      .comparison__subtitle {
        font-size: 13px;
        color: var(--text-secondary);
      }

      .comparison__elapsed {
        display: inline-flex;
        align-items: center;
        gap: var(--space-1);
        font-size: 12px;
        color: var(--text-muted);

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      .comparison-scroll {
        overflow-x: auto;
        border: 1px solid var(--border);
        border-radius: var(--radius-md);
      }

      // Toolkit table: 40px muted header over a control-colour rule, hairline rows.
      .comparison__table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;

        th {
          height: 40px;
          padding: var(--space-1) var(--space-3);
          text-align: right;
          vertical-align: middle;
          font-size: 12px;
          line-height: 16px;
          font-weight: 500;
          color: var(--text-muted);
          background: var(--surface);
          border-bottom: 1px solid var(--border-control);
          white-space: nowrap;
        }

        th.comparison__metric-head {
          text-align: left;
        }

        td {
          height: 40px;
          padding: 0 var(--space-3);
          text-align: right;
          color: var(--text);
          border-bottom: 1px solid var(--border);
          white-space: nowrap;
        }

        td.mono {
          font-weight: 500;
        }

        tbody tr:hover td {
          background: var(--surface-sunken);
        }

        tbody tr:last-child td {
          border-bottom: none;
        }
      }

      .comparison__col-date {
        display: block;
        font-size: 11px;
        font-weight: 400;
        color: var(--text-muted);
      }

      .comparison__table td.comparison__metric-label {
        text-align: left;
        color: var(--text);
        font-weight: 500;
      }

      .comparison__change {
        display: inline-flex;
        align-items: center;
        justify-content: flex-end;
        gap: 2px;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      .comparison__table td.good,
      .good {
        color: var(--success-text);
      }

      .comparison__table td.bad,
      .bad {
        color: var(--danger-text);
      }

      .comparison__table td.neutral,
      .neutral {
        color: var(--primary-text);
      }

      // Since This Snapshot — a success-tinted summary band beneath the table.
      .since-snapshot {
        margin-top: var(--space-4);
        padding: var(--space-4);
        border: 1px solid color-mix(in srgb, var(--success) 28%, transparent);
        border-radius: var(--radius-md);
        background: color-mix(in srgb, var(--success) 7%, transparent);
      }

      .since-snapshot__header {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        margin-bottom: var(--space-3);
        font-size: 14px;
        line-height: 20px;
        font-weight: 600;
        color: var(--success-text);

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }
      }

      .since-snapshot__grid {
        display: grid;
        // Six regular cells plus a wider closing column for Adjusted Capital Growth, all on one row.
        grid-template-columns: repeat(6, minmax(0, 1fr)) minmax(0, 1.6fr);
        gap: var(--space-3) var(--space-3);
      }

      .since-snapshot__cell {
        display: flex;
        flex-direction: column;
        gap: var(--space-1);
        min-width: 0;
      }

      .since-snapshot__label {
        font-size: 12px;
        line-height: 16px;
        color: var(--text-muted);
        white-space: nowrap;
      }

      .since-snapshot__value {
        display: inline-flex;
        align-items: center;
        gap: 2px;
        font-size: 13px;
        font-weight: 600;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      // Adjusted Capital Growth — the row's last, wider column, set apart as the headline figure.
      .since-snapshot__cell--highlight {
        padding-left: var(--space-3);
        border-left: 1px solid color-mix(in srgb, var(--success) 35%, transparent);
      }

      .since-snapshot__label--caps {
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--text-secondary);
      }

      .since-snapshot__value--large {
        font-size: 18px;
        line-height: 24px;
        font-weight: 700;

        mat-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
        }
      }

      .since-snapshot__subtext {
        font-size: 12px;
        color: var(--text-muted);
      }

      @media (max-width: 899px) {
        .since-snapshot__grid {
          grid-template-columns: repeat(3, minmax(0, 1fr));
        }

        .since-snapshot__cell--highlight {
          grid-column: 1 / -1;
          padding-left: 0;
          padding-top: var(--space-3);
          border-left: none;
          border-top: 1px solid color-mix(in srgb, var(--success) 35%, transparent);
        }
      }

      @media (max-width: 599px) {
        .since-snapshot__grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
      }
    `,
  ],
})
export class FinancialComparisonComponent {
  @Input() comparison: FinancialComparison | null = null;

  private isCount(metric: FinancialComparisonMetric): boolean {
    return COUNT_KEYS.has(metric.key);
  }

  metric(key: string): FinancialComparisonMetric | undefined {
    return this.comparison?.metrics.find((m) => m.key === key);
  }

  formatValue(metric: FinancialComparisonMetric, current = false): string {
    const value = current ? metric.currentValue : metric.snapshotValue;
    return this.isCount(metric) ? `${value}` : this.formatMoney(value);
  }

  /** Calendar time between snapshotDate and todayDate (both yyyy-MM-dd), e.g. "Captured 2 months, 5 days ago". */
  elapsedText(): string {
    if (!this.comparison) return '';
    const [y1, m1, d1] = this.comparison.snapshotDate.split('-').map(Number);
    const [y2, m2, d2] = this.comparison.todayDate.split('-').map(Number);

    let months = (y2 - y1) * 12 + (m2 - m1);
    if (d2 < d1) months--;
    if (months < 0) return 'Captured today';

    // Days left over after stepping forward whole months; Date.UTC normalizes day overflow (e.g. Jan 31 + 1 month).
    const anchor = Date.UTC(y1, m1 - 1 + months, d1);
    const days = Math.round((Date.UTC(y2, m2 - 1, d2) - anchor) / 86_400_000);

    const years = Math.floor(months / 12);
    const remMonths = months % 12;
    const plural = (n: number, unit: string) => `${n} ${unit}${n === 1 ? '' : 's'}`;

    const parts: string[] = [];
    if (years > 0) parts.push(plural(years, 'year'));
    if (remMonths > 0) parts.push(plural(remMonths, 'month'));
    if (days > 0 && years === 0) parts.push(plural(days, 'day'));

    return parts.length === 0 ? 'Captured today' : `Captured ${parts.join(', ')} ago`;
  }

  formatMoney(value: number): string {
    return `₱${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatChange(metric: FinancialComparisonMetric): string {
    const sign = metric.change > 0 ? '+' : metric.change < 0 ? '-' : '';
    const magnitude = Math.abs(metric.change);
    return this.isCount(metric) ? `${sign}${magnitude}` : `${sign}${this.formatMoney(magnitude)}`;
  }

  formatSignedMoney(value: number): string {
    const sign = value > 0 ? '+' : value < 0 ? '-' : '';
    return `${sign}${this.formatMoney(Math.abs(value))}`;
  }

  formatSignedPercent(value: number | null): string {
    if (value === null) return 'N/A';
    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(2)}%`;
  }

  /** Plain sign-based coloring, for figures where growth is always the good direction (e.g. Adjusted Capital Growth). */
  signClass(value: number): 'good' | 'bad' | 'neutral' {
    return value > 0 ? 'good' : value < 0 ? 'bad' : 'neutral';
  }

  changeIcon(change: number): string {
    return change > 0 ? 'arrow_upward' : change < 0 ? 'arrow_downward' : 'remove';
  }

  /** Requirements §16 — snapshot value of zero renders "New" when the metric now has a value, otherwise "N/A". */
  formatPercent(metric: FinancialComparisonMetric): string {
    if (metric.percentChange === null) {
      return metric.currentValue !== 0 ? 'New' : 'N/A';
    }
    const sign = metric.percentChange > 0 ? '+' : '';
    return `${sign}${metric.percentChange.toFixed(1)}%`;
  }

  /** Requirements §17's contextual coloring, driven by each metric's own direction rule rather than treating every positive change as "good". */
  colorClass(metric: FinancialComparisonMetric): 'good' | 'bad' | 'neutral' {
    const direction = METRIC_DIRECTION[metric.key] ?? 'neutral';
    if (direction === 'neutral' || metric.change === 0) return 'neutral';
    const isGood = direction === 'lowerIsBetter' ? metric.change < 0 : metric.change > 0;
    return isGood ? 'good' : 'bad';
  }

  exportCsv(): void {
    if (!this.comparison) return;
    const header = ['Metric', `Snapshot (${this.comparison.snapshotDate})`, `Today (${this.comparison.todayDate})`, 'Change', '% Change'];
    const rows = this.comparison.metrics.map((m) => [
      m.label,
      this.formatValue(m),
      this.formatValue(m, true),
      this.formatChange(m),
      this.formatPercent(m),
    ]);
    const csv = [header, ...rows].map((row) => row.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\r\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `financial-comparison_${this.comparison.snapshotDate}_vs_${this.comparison.todayDate}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }
}
