import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DiaryFinancialSnapshot } from '../../domain/entities/diary-entry.entity';

/** Requirements §14 — the Financial Snapshot display block, reused by both the Diary timeline's inline summary and the Diary Entry Detail page. */
@Component({
  selector: 'lm-financial-snapshot',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="snapshot" *ngIf="snapshot">
      <div class="snapshot__header">
        <span class="snapshot__title">FINANCIAL SNAPSHOT</span>
        <span class="snapshot__date">{{ snapshot.snapshotDateTime | date: "MMMM d, y 'at' h:mm a" }}</span>
      </div>

      <div class="snapshot__grid">
        <div class="snapshot__row">
          <span>Gross Receivables</span>
          <span class="mono">₱{{ snapshot.grossReceivables | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Collectible Receivables</span>
          <span class="mono">₱{{ snapshot.collectibleReceivables | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Bad Loan Receivables</span>
          <span class="mono">₱{{ snapshot.badLoanReceivables | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Cash on Hand</span>
          <span class="mono">₱{{ snapshot.cashOnHand | number: '1.2-2' }}</span>
        </div>
      </div>

      <div class="snapshot__grid">
        <div class="snapshot__row">
          <span>Active Loans</span>
          <span class="mono">{{ snapshot.activeLoanCount }}</span>
        </div>
        <div class="snapshot__row">
          <span>Overdue Loans</span>
          <span class="mono">{{ snapshot.overdueLoanCount }}</span>
        </div>
        <div class="snapshot__row">
          <span>Bad Loans</span>
          <span class="mono">{{ snapshot.badLoanCount }}</span>
        </div>
      </div>

      <div class="snapshot__grid">
        <div class="snapshot__row">
          <span>Collections Today</span>
          <span class="mono">₱{{ snapshot.collectionsToday | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Collections MTD</span>
          <span class="mono">₱{{ snapshot.collectionsMonthToDate | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Loans Released Today</span>
          <span class="mono">₱{{ snapshot.loanReleasesToday | number: '1.2-2' }}</span>
        </div>
        <div class="snapshot__row">
          <span>Loans Released MTD</span>
          <span class="mono">₱{{ snapshot.loanReleasesMonthToDate | number: '1.2-2' }}</span>
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
        background: var(--lm-bg);
        border: 1px solid var(--lm-border);
        border-radius: var(--lm-radius-control);
        padding: 14px 16px;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .snapshot__header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        flex-wrap: wrap;
        gap: 6px;
      }

      .snapshot__title {
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.04em;
        color: var(--lm-text-muted);
      }

      .snapshot__date {
        font-size: 12px;
        color: var(--lm-text-muted);
      }

      .snapshot__grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 6px 20px;
        padding-top: 10px;
        border-top: 1px solid var(--lm-border);

        &:first-of-type {
          border-top: none;
          padding-top: 0;
        }
      }

      .snapshot__row {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        flex-wrap: wrap;
        gap: 2px 10px;
        font-size: 13px;
        min-width: 0;

        span:first-child {
          color: var(--lm-text-muted);
        }

        span:last-child {
          font-weight: 600;
          color: var(--lm-text);
          white-space: nowrap;
        }
      }

      // Two label/value pairs per row get too tight for peso amounts once
      // the card itself narrows on a phone — each pair drops to its own
      // full-width row instead of clipping the value at the card edge.
      @media (max-width: 599px) {
        .snapshot__grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class FinancialSnapshotComponent {
  @Input() snapshot: DiaryFinancialSnapshot | null = null;
}
