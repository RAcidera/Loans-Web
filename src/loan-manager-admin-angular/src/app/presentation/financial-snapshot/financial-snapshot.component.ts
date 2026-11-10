import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DiaryFinancialSnapshot } from '../../domain/entities/diary-entry.entity';
import { AppDateTimePipe } from '../shared/app-date-time.pipe';

/** Requirements §14 — the Financial Snapshot display block, reused by both the Diary timeline's inline summary and the Diary Entry Detail page. Redesigned per design assets/diary-detail-snapshot.png: a KPI card row for the four headline figures, a compact stat row for loan counts + today's activity, and an MTD row — instead of the old plain label/value grid. */
@Component({
  selector: 'lm-financial-snapshot',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule, AppDateTimePipe],
  template: `
    <div class="snapshot" *ngIf="snapshot">
      <div class="snapshot__header">
        <div class="snapshot__title-group">
          <span class="snapshot__title">Financial Snapshot</span>
          <mat-icon class="snapshot__info" matTooltip="Captured figures are immutable — they never change after the entry is saved." aria-hidden="false">info_outline</mat-icon>
        </div>
        <div class="snapshot__header-right">
          <span class="snapshot__date">Captured on {{ snapshot.snapshotDateTime | appDateTime: "MMM d, y 'at' h:mm a" }}</span>
          <button mat-flat-button color="primary" (click)="compare.emit()" [disabled]="comparing">
            <mat-icon>trending_up</mat-icon>
            {{ comparing ? 'Comparing…' : 'Compare to Today' }}
          </button>
        </div>
      </div>

      <div class="kpi-grid">
        <div class="kpi-card kpi-card--primary snap-kpi">
          <span class="kpi-card__icon"><mat-icon>account_balance_wallet</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Gross Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.grossReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="snap-kpi__bar"></span>
        </div>
        <div class="kpi-card kpi-card--good snap-kpi">
          <span class="kpi-card__icon"><mat-icon>fact_check</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Collectible Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.collectibleReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="snap-kpi__bar"></span>
        </div>
        <div class="kpi-card kpi-card--danger snap-kpi">
          <span class="kpi-card__icon"><mat-icon>report_problem</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Bad Loan Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.badLoanReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="snap-kpi__bar"></span>
        </div>
        <div class="kpi-card kpi-card--info snap-kpi">
          <span class="kpi-card__icon"><mat-icon>payments</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Cash on Hand</span>
            <span class="kpi-card__value mono">₱{{ snapshot.cashOnHand | number: '1.2-2' }}</span>
          </span>
          <span class="snap-kpi__bar"></span>
        </div>
      </div>

      <div class="stat-row">
        <div class="stat-cell">
          <span class="stat-cell__icon stat-cell__icon--info"><mat-icon>assignment</mat-icon></span>
          <span class="stat-cell__text">
            <span class="stat-cell__label">Active Loans</span>
            <span class="stat-cell__value mono">{{ snapshot.activeLoanCount }}</span>
          </span>
        </div>
        <div class="stat-cell">
          <span class="stat-cell__icon stat-cell__icon--warning"><mat-icon>schedule</mat-icon></span>
          <span class="stat-cell__text">
            <span class="stat-cell__label">Overdue Loans</span>
            <span class="stat-cell__value mono">{{ snapshot.overdueLoanCount }}</span>
          </span>
        </div>
        <div class="stat-cell">
          <span class="stat-cell__icon stat-cell__icon--danger"><mat-icon>warning</mat-icon></span>
          <span class="stat-cell__text">
            <span class="stat-cell__label">Bad Loans</span>
            <span class="stat-cell__value mono">{{ snapshot.badLoanCount }}</span>
          </span>
        </div>
        <div class="stat-cell">
          <span class="stat-cell__icon stat-cell__icon--success"><mat-icon>paid</mat-icon></span>
          <span class="stat-cell__text">
            <span class="stat-cell__label">Collections Today</span>
            <span class="stat-cell__value mono">₱{{ snapshot.collectionsToday | number: '1.0-0' }}</span>
          </span>
        </div>
        <div class="stat-cell">
          <span class="stat-cell__icon stat-cell__icon--primary"><mat-icon>request_quote</mat-icon></span>
          <span class="stat-cell__text">
            <span class="stat-cell__label">Loans Released Today</span>
            <span class="stat-cell__value mono">₱{{ snapshot.loanReleasesToday | number: '1.0-0' }}</span>
          </span>
        </div>
      </div>

      <div class="mtd-row">
        <div class="mtd-cell">
          <div>
            <span class="mtd-cell__label mtd-cell__label--success">Collections MTD</span>
            <span class="mtd-cell__value mono">₱{{ snapshot.collectionsMonthToDate | number: '1.2-2' }}</span>
          </div>
          <span class="mtd-cell__trend mtd-cell__trend--success"><mat-icon>trending_up</mat-icon></span>
        </div>
        <div class="mtd-cell">
          <div>
            <span class="mtd-cell__label mtd-cell__label--primary">Loans Released MTD</span>
            <span class="mtd-cell__value mono">₱{{ snapshot.loanReleasesMonthToDate | number: '1.2-2' }}</span>
          </div>
          <span class="mtd-cell__trend mtd-cell__trend--primary"><mat-icon>trending_up</mat-icon></span>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      // Headline figures use the global page KPI card (.kpi-card + tone
      // modifiers, styles/_page-patterns.scss); only the snapshot-specific
      // tone underline bar, stat row and MTD row are styled here.
      .snapshot {
        display: flex;
        flex-direction: column;
        gap: var(--space-4);
      }

      .snapshot__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: var(--space-3);
      }

      .snapshot__title-group {
        display: flex;
        align-items: center;
        gap: var(--space-1);
      }

      .snapshot__title {
        font-size: 14px;
        line-height: 20px;
        font-weight: 600;
        color: var(--text);
      }

      .snapshot__info {
        font-size: 16px;
        width: 16px;
        height: 16px;
        color: var(--text-muted);
        cursor: help;
      }

      .snapshot__header-right {
        display: flex;
        align-items: center;
        gap: var(--space-3);
        flex-wrap: wrap;
      }

      .snapshot__date {
        font-size: 12px;
        color: var(--text-muted);
      }

      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: var(--space-3);
      }

      .snap-kpi {
        position: relative;
        overflow: hidden;
      }

      .kpi-card__text {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }

      // Tone underline bar along the card's bottom edge.
      .snap-kpi__bar {
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        height: 3px;
        background: var(--primary);
      }

      .kpi-card--good .snap-kpi__bar { background: var(--success); }
      .kpi-card--danger .snap-kpi__bar { background: var(--danger); }
      .kpi-card--info .snap-kpi__bar { background: var(--info); }

      // Stat row — loan counts + today's activity, five compact cells.
      .stat-row {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: var(--space-3);
        padding: var(--space-3);
        border: 1px solid var(--border);
        border-radius: var(--radius-md);
        background: var(--surface-sunken);
      }

      .stat-cell {
        display: flex;
        align-items: center;
        gap: var(--space-2);
        min-width: 0;

        &__icon {
          width: 28px;
          height: 28px;
          flex-shrink: 0;
          display: grid;
          place-items: center;
          border-radius: 50%;

          mat-icon {
            font-size: 16px;
            width: 16px;
            height: 16px;
          }

          &--primary { background: var(--primary-subtle); color: var(--primary-text); }
          &--info { background: var(--info-subtle); color: var(--info-text); }
          &--warning { background: var(--warning-subtle); color: var(--warning-text); }
          &--danger { background: var(--danger-subtle); color: var(--danger-text); }
          &--success { background: var(--success-subtle); color: var(--success-text); }
        }

        &__text {
          display: flex;
          flex-direction: column;
          min-width: 0;
        }

        &__label {
          font-size: 12px;
          line-height: 16px;
          color: var(--text-muted);
        }

        &__value {
          font-size: 14px;
          line-height: 20px;
          font-weight: 600;
          color: var(--text);
          white-space: nowrap;
        }
      }

      // MTD row — two wide cells with a trend glyph.
      .mtd-row {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: var(--space-3);
      }

      .mtd-cell {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-3);
        padding: var(--space-3) var(--space-4);
        border: 1px solid var(--border);
        border-radius: var(--radius-md);

        > div {
          display: flex;
          flex-direction: column;
          gap: 2px;
        }

        &__label {
          font-size: 12px;
          line-height: 16px;
          font-weight: 600;

          &--success { color: var(--success-text); }
          &--primary { color: var(--primary-text); }
        }

        &__value {
          font-size: 16px;
          line-height: 22px;
          font-weight: 700;
          color: var(--text);
        }

        &__trend {
          width: 32px;
          height: 32px;
          flex-shrink: 0;
          display: grid;
          place-items: center;
          border-radius: 50%;

          mat-icon {
            font-size: 16px;
            width: 16px;
            height: 16px;
          }

          &--success { background: var(--success-subtle); color: var(--success-text); }
          &--primary { background: var(--primary-subtle); color: var(--primary-text); }
        }
      }

      @media (max-width: 899px) {
        .kpi-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
        .stat-row {
          grid-template-columns: repeat(3, minmax(0, 1fr));
        }
      }

      @media (max-width: 599px) {
        .kpi-grid {
          grid-template-columns: 1fr;
        }
        .stat-row {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
        .mtd-row {
          grid-template-columns: 1fr;
        }
        .snapshot__header-right {
          width: 100%;
          justify-content: space-between;
        }
      }
    `,
  ],
})
export class FinancialSnapshotComponent {
  @Input() snapshot: DiaryFinancialSnapshot | null = null;
  @Input() comparing = false;
  @Output() compare = new EventEmitter<void>();
}
