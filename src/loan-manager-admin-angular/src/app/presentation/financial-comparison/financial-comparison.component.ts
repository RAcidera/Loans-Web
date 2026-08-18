import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FinancialComparison, FinancialComparisonMetric } from '../../domain/entities/diary-entry.entity';

/** Which direction of change is a "good" outcome for a given metric key — requirements §17. Money-shaped keys render with a peso sign; count-shaped keys render as plain numbers. */
type Direction = 'lowerIsBetter' | 'higherIsBetter' | 'neutral';

const METRIC_DIRECTION: Record<string, Direction> = {
  grossReceivables: 'neutral', // requirements §17's own example of "not inherently good or bad"
  collectibleReceivables: 'neutral',
  badLoanReceivables: 'lowerIsBetter',
  cashOnHand: 'higherIsBetter',
  activeLoanCount: 'neutral',
  overdueLoanCount: 'lowerIsBetter',
  badLoanCount: 'lowerIsBetter',
};

const COUNT_KEYS = new Set(['activeLoanCount', 'overdueLoanCount', 'badLoanCount']);

/** Requirements §15-17 — the Snapshot vs. Today vs. Change vs. % comparison table, with contextual coloring driven by each metric's own direction rule rather than a blanket positive/negative-number rule. */
@Component({
  selector: 'lm-financial-comparison',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="comparison" *ngIf="comparison">
      <div class="comparison__header">
        <div>
          <span class="comparison__label">Snapshot</span>
          <span class="comparison__date">{{ comparison.snapshotDate | date: 'MMM d, y' }}</span>
        </div>
        <div>
          <span class="comparison__label">Today</span>
          <span class="comparison__date">{{ comparison.todayDate | date: 'MMM d, y' }}</span>
        </div>
      </div>

      <div class="comparison-scroll">
        <table class="comparison__table">
          <thead>
            <tr>
              <th></th>
              <th>Snapshot</th>
              <th>Today</th>
              <th>Change</th>
              <th>%</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let metric of comparison.metrics">
              <td class="comparison__metric-label">{{ metric.label }}</td>
              <td class="mono">{{ formatValue(metric) }}</td>
              <td class="mono">{{ formatValue(metric, true) }}</td>
              <td class="mono" [ngClass]="colorClass(metric)">{{ formatChange(metric) }}</td>
              <td class="mono" [ngClass]="colorClass(metric)">{{ formatPercent(metric) }}</td>
            </tr>
          </tbody>
        </table>
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
        gap: 24px;
        margin-bottom: 12px;

        > div {
          display: flex;
          flex-direction: column;
          gap: 2px;
        }
      }

      .comparison__label {
        font-size: 11px;
        font-weight: 700;
        color: var(--lm-text-muted);
        letter-spacing: 0.03em;
      }

      .comparison__date {
        font-size: 14px;
        font-weight: 600;
        color: var(--lm-text);
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
          padding: 6px 10px;
          border-bottom: 1px solid var(--lm-border);

          &:first-child {
            text-align: left;
          }
        }

        td {
          text-align: right;
          padding: 8px 10px;
          border-bottom: 1px solid var(--lm-border);
        }
      }

      .comparison__metric-label {
        text-align: left !important;
        color: var(--lm-text);
        font-weight: 500;
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
    `,
  ],
})
export class FinancialComparisonComponent {
  @Input() comparison: FinancialComparison | null = null;

  private isCount(metric: FinancialComparisonMetric): boolean {
    return COUNT_KEYS.has(metric.key);
  }

  formatValue(metric: FinancialComparisonMetric, current = false): string {
    const value = current ? metric.currentValue : metric.snapshotValue;
    return this.isCount(metric) ? `${value}` : `₱${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatChange(metric: FinancialComparisonMetric): string {
    const sign = metric.change > 0 ? '+' : metric.change < 0 ? '-' : '';
    const magnitude = Math.abs(metric.change);
    return this.isCount(metric)
      ? `${sign}${magnitude}`
      : `${sign}₱${magnitude.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
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
}
