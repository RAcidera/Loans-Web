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
          <span class="comparison__title">FINANCIAL COMPARISON</span>
          <span class="comparison__subtitle">
            Snapshot vs Today ({{ comparison.todayDate | appDate: 'MMM d, y' }})
          </span>
        </div>
        <button mat-stroked-button class="comparison__export-btn" (click)="exportCsv()">
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
              <th>Change</th>
              <th>% Change</th>
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
          <span>SINCE THIS SNAPSHOT</span>
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
          <div class="since-snapshot__cell" *ngIf="metric('overdueLoanCount') as m">
            <span class="since-snapshot__label">Overdue Loans</span>
            <span class="since-snapshot__value" [ngClass]="colorClass(m)">
              <span class="mono">{{ m.snapshotValue }} &rarr; {{ m.currentValue }}</span>
              <mat-icon *ngIf="m.change !== 0">{{ changeIcon(m.change) }}</mat-icon>
            </span>
          </div>
          <div class="since-snapshot__cell" *ngIf="metric('badLoanCount') as m">
            <span class="since-snapshot__label">Bad Loans</span>
            <span class="since-snapshot__value" [ngClass]="colorClass(m)">
              <span class="mono">{{ m.snapshotValue }} &rarr; {{ m.currentValue }}</span>
              <mat-icon *ngIf="m.change !== 0">{{ changeIcon(m.change) }}</mat-icon>
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .mono {
        font-family: var(--lm-font-mono);
        font-feature-settings: 'tnum' 1;
      }

      .comparison__header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 12px;
        flex-wrap: wrap;
        margin-bottom: 14px;
      }

      .comparison__heading {
        display: flex;
        flex-direction: column;
        gap: 3px;
      }

      .comparison__title {
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--lm-text-muted);
      }

      .comparison__subtitle {
        font-size: 13px;
        font-weight: 600;
        color: var(--lm-text);
      }

      .comparison__export-btn {
        display: flex;
        align-items: center;
        gap: 4px;

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }
      }

      .comparison-scroll {
        overflow-x: auto;
      }

      .comparison__table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px;

        th {
          text-align: right;
          font-size: 11px;
          font-weight: 700;
          color: var(--lm-text-muted);
          padding: 8px 10px;
          border-bottom: 1px solid var(--lm-border);
          background: var(--lm-bg);
        }

        th.comparison__metric-head {
          text-align: left;
        }

        td {
          text-align: right;
          padding: 9px 10px;
          border-bottom: 1px solid var(--lm-border);
        }

        // Snapshot/Today/Change/% Change figures — heavier than the table's plain
        // 400 default so they read as the primary data, not the Metric label.
        td.mono {
          font-weight: 500;
        }

        tbody tr:last-child td {
          border-bottom: none;
        }
      }

      .comparison__col-date {
        display: block;
        font-size: 10.5px;
        font-weight: 500;
        text-transform: none;
        letter-spacing: normal;
        color: var(--lm-text-muted);
        margin-top: 1px;
      }

      .comparison__metric-label {
        text-align: left !important;
        color: var(--lm-text);
        font-weight: 500;
      }

      .comparison__change {
        display: inline-flex;
        align-items: center;
        justify-content: flex-end;
        gap: 3px;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      .good {
        color: var(--lm-success);
      }

      .bad {
        color: var(--lm-danger);
      }

      .neutral {
        color: var(--lm-primary);
      }

      // Since This Snapshot — a tinted summary band beneath the table.
      .since-snapshot {
        margin-top: 16px;
        background: rgba(34, 160, 107, 0.06);
        border: 1px solid rgba(34, 160, 107, 0.2);
        border-radius: var(--lm-radius-control);
        padding: 14px 16px;
      }

      .since-snapshot__header {
        display: flex;
        align-items: center;
        gap: 6px;
        margin-bottom: 12px;
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--lm-success);

        mat-icon {
          font-size: 15px;
          width: 15px;
          height: 15px;
        }
      }

      .since-snapshot__grid {
        display: grid;
        grid-template-columns: repeat(6, 1fr);
        gap: 12px 10px;
      }

      .since-snapshot__cell {
        display: flex;
        flex-direction: column;
        gap: 4px;
        min-width: 0;
      }

      .since-snapshot__label {
        font-size: 11px;
        color: var(--lm-text-muted);
        white-space: nowrap;
      }

      .since-snapshot__value {
        display: inline-flex;
        align-items: center;
        gap: 3px;
        font-size: 13px;
        font-weight: 700;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      @media (max-width: 899px) {
        .since-snapshot__grid {
          grid-template-columns: repeat(3, 1fr);
        }
      }

      @media (max-width: 599px) {
        .since-snapshot__grid {
          grid-template-columns: repeat(2, 1fr);
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

  formatMoney(value: number): string {
    return `₱${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatChange(metric: FinancialComparisonMetric): string {
    const sign = metric.change > 0 ? '+' : metric.change < 0 ? '-' : '';
    const magnitude = Math.abs(metric.change);
    return this.isCount(metric) ? `${sign}${magnitude}` : `${sign}${this.formatMoney(magnitude)}`;
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
