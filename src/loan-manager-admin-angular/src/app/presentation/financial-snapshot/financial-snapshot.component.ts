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
          <span class="snapshot__title">FINANCIAL SNAPSHOT</span>
          <mat-icon class="snapshot__info" matTooltip="Captured figures are immutable — they never change after the entry is saved." aria-hidden="false">info_outline</mat-icon>
        </div>
        <div class="snapshot__header-right">
          <span class="snapshot__date">Captured on {{ snapshot.snapshotDateTime | appDateTime: "MMM d, y 'at' h:mm a" }}</span>
          <button mat-flat-button color="primary" class="snapshot__compare-btn" (click)="compare.emit()" [disabled]="comparing">
            <mat-icon>trending_up</mat-icon>
            {{ comparing ? 'Comparing…' : 'Compare to Today' }}
          </button>
        </div>
      </div>

      <div class="kpi-grid">
        <div class="kpi-card kpi-card--primary">
          <span class="kpi-card__icon"><mat-icon>account_balance_wallet</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Gross Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.grossReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="kpi-card__bar"></span>
        </div>
        <div class="kpi-card kpi-card--success">
          <span class="kpi-card__icon"><mat-icon>fact_check</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Collectible Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.collectibleReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="kpi-card__bar"></span>
        </div>
        <div class="kpi-card kpi-card--danger">
          <span class="kpi-card__icon"><mat-icon>report_problem</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Bad Loan Receivables</span>
            <span class="kpi-card__value mono">₱{{ snapshot.badLoanReceivables | number: '1.2-2' }}</span>
          </span>
          <span class="kpi-card__bar"></span>
        </div>
        <div class="kpi-card kpi-card--info">
          <span class="kpi-card__icon"><mat-icon>payments</mat-icon></span>
          <span class="kpi-card__text">
            <span class="kpi-card__label">Cash on Hand</span>
            <span class="kpi-card__value mono">₱{{ snapshot.cashOnHand | number: '1.2-2' }}</span>
          </span>
          <span class="kpi-card__bar"></span>
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
      .mono {
        font-family: var(--lm-font-mono);
        font-feature-settings: 'tnum' 1;
      }

      .snapshot {
        display: flex;
        flex-direction: column;
        gap: 14px;
      }

      .snapshot__header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 10px;
      }

      .snapshot__title-group {
        display: flex;
        align-items: center;
        gap: 5px;
      }

      .snapshot__title {
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--lm-text-muted);
      }

      .snapshot__info {
        font-size: 15px;
        width: 15px;
        height: 15px;
        color: var(--lm-text-muted);
        cursor: help;
      }

      .snapshot__header-right {
        display: flex;
        align-items: center;
        gap: 12px;
        flex-wrap: wrap;
      }

      .snapshot__date {
        font-size: 12px;
        color: var(--lm-text-muted);
      }

      .snapshot__compare-btn {
        display: flex;
        align-items: center;
        gap: 4px;
        line-height: 1;

        mat-icon {
          font-size: 17px;
          width: 17px;
          height: 17px;
        }
      }

      // KPI cards — four headline figures, each with an icon badge and a
      // colored underline bar (design assets/diary-detail-snapshot.png).
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 12px;
      }

      .kpi-card {
        position: relative;
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 10px;
        background: var(--lm-surface);
        border: 1px solid var(--lm-border);
        border-radius: var(--lm-radius-control);
        padding: 10px 14px 12px;
        overflow: hidden;

        &__icon {
          width: 32px;
          height: 32px;
          border-radius: 10px;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;

          mat-icon {
            font-size: 17px;
            width: 17px;
            height: 17px;
          }
        }

        &__text {
          display: flex;
          flex-direction: column;
          gap: 2px;
          min-width: 0;
        }

        &__label {
          font-size: 11.5px;
          font-weight: 600;
          color: var(--lm-text-muted);
        }

        &__value {
          font-size: 17px;
          font-weight: 700;
          color: var(--lm-text);
          white-space: nowrap;
        }

        &__bar {
          position: absolute;
          left: 0;
          right: 0;
          bottom: 0;
          height: 3px;
        }

        &--primary {
          .kpi-card__icon {
            background: rgba(108, 93, 211, 0.12);
            color: var(--lm-primary);
          }
          .kpi-card__bar {
            background: var(--lm-primary);
          }
        }

        &--success {
          .kpi-card__icon {
            background: rgba(34, 160, 107, 0.12);
            color: var(--lm-success);
          }
          .kpi-card__bar {
            background: var(--lm-success);
          }
        }

        &--danger {
          .kpi-card__icon {
            background: rgba(193, 102, 107, 0.12);
            color: var(--lm-danger);
          }
          .kpi-card__bar {
            background: var(--lm-danger);
          }
        }

        &--info {
          .kpi-card__icon {
            background: rgba(76, 141, 255, 0.12);
            color: var(--lm-info);
          }
          .kpi-card__bar {
            background: var(--lm-info);
          }
        }
      }

      // Stat row — loan counts + today's activity, five compact cells.
      .stat-row {
        display: grid;
        grid-template-columns: repeat(5, 1fr);
        gap: 10px;
        background: var(--lm-bg);
        border: 1px solid var(--lm-border);
        border-radius: var(--lm-radius-control);
        padding: 10px;
      }

      .stat-cell {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 8px;
        text-align: left;
        min-width: 0;

        &__icon {
          width: 28px;
          height: 28px;
          border-radius: 999px;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;

          mat-icon {
            font-size: 15px;
            width: 15px;
            height: 15px;
          }

          &--primary {
            background: rgba(108, 93, 211, 0.12);
            color: var(--lm-primary);
          }
          &--info {
            background: rgba(76, 141, 255, 0.12);
            color: var(--lm-info);
          }
          &--warning {
            background: rgba(225, 170, 54, 0.15);
            color: var(--lm-warning);
          }
          &--danger {
            background: rgba(193, 102, 107, 0.12);
            color: var(--lm-danger);
          }
          &--success {
            background: rgba(34, 160, 107, 0.12);
            color: var(--lm-success);
          }
        }

        &__text {
          display: flex;
          flex-direction: column;
          gap: 1px;
          min-width: 0;
        }

        &__label {
          font-size: 10.5px;
          color: var(--lm-text-muted);
          line-height: 1.2;
        }

        &__value {
          font-size: 14px;
          font-weight: 700;
          color: var(--lm-text);
          white-space: nowrap;
        }
      }

      // MTD row — two wide cells with a trend glyph.
      .mtd-row {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 12px;
      }

      .mtd-cell {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 10px;
        border: 1px solid var(--lm-border);
        border-radius: var(--lm-radius-control);
        padding: 12px 16px;

        > div {
          display: flex;
          flex-direction: column;
          gap: 4px;
        }

        &__label {
          font-size: 12px;
          font-weight: 600;

          &--success {
            color: var(--lm-success);
          }
          &--primary {
            color: var(--lm-primary);
          }
        }

        &__value {
          font-size: 16px;
          font-weight: 700;
          color: var(--lm-text);
        }

        &__trend {
          width: 30px;
          height: 30px;
          border-radius: 999px;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;

          mat-icon {
            font-size: 16px;
            width: 16px;
            height: 16px;
          }

          &--success {
            background: rgba(34, 160, 107, 0.12);
            color: var(--lm-success);
          }
          &--primary {
            background: rgba(108, 93, 211, 0.12);
            color: var(--lm-primary);
          }
        }
      }

      @media (max-width: 899px) {
        .kpi-grid {
          grid-template-columns: repeat(2, 1fr);
        }
        .stat-row {
          grid-template-columns: repeat(3, 1fr);
          row-gap: 14px;
        }
      }

      @media (max-width: 599px) {
        .kpi-grid {
          grid-template-columns: 1fr;
        }
        .stat-row {
          grid-template-columns: repeat(2, 1fr);
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
