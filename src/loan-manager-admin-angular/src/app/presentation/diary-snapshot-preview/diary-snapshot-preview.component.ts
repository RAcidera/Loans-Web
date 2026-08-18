import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DiaryFinancialSnapshot } from '../../domain/entities/diary-entry.entity';

/**
 * Requirements diary-modern §12's Snapshot Preview — the Timeline card's
 * COMPACT contextual panel, distinct from the full financial-snapshot
 * component the Detail page uses (§14, all 12 fields). Only the four key
 * figures the requirement calls out, so the card stays short ("Keep cards
 * compact and avoid large empty areas," §27) — the rest is one click away
 * via the View button.
 */
@Component({
  selector: 'lm-diary-snapshot-preview',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="preview" *ngIf="snapshot">
      <div class="preview__header">
        <span class="preview__title">Financial Snapshot</span>
        <span class="preview__time mono">{{ snapshot.snapshotDateTime | date: 'h:mm a' }}</span>
      </div>
      <div class="preview__row">
        <span>Gross Receivables</span>
        <span class="mono">₱{{ snapshot.grossReceivables | number: '1.0-0' }}</span>
      </div>
      <div class="preview__row">
        <span>Collectible</span>
        <span class="mono">₱{{ snapshot.collectibleReceivables | number: '1.0-0' }}</span>
      </div>
      <div class="preview__row">
        <span>Bad Loan Receivables</span>
        <span class="mono">₱{{ snapshot.badLoanReceivables | number: '1.0-0' }}</span>
      </div>
      <div class="preview__row">
        <span>Cash on Hand</span>
        <span class="mono">₱{{ snapshot.cashOnHand | number: '1.0-0' }}</span>
      </div>
    </div>
  `,
  styles: [
    `
      .mono {
        font-family: var(--lm-font-mono);
        font-feature-settings: 'tnum' 1;
      }

      .preview {
        display: flex;
        flex-direction: column;
        gap: 5px;
      }

      .preview__header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        margin-bottom: 2px;
      }

      .preview__title {
        font-size: 10.5px;
        font-weight: 700;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        color: var(--lm-text-muted);
      }

      .preview__time {
        font-size: 10.5px;
        color: var(--lm-text-muted);
      }

      .preview__row {
        display: flex;
        justify-content: space-between;
        gap: 10px;
        font-size: 12px;

        span:first-child {
          color: var(--lm-text-muted);
        }

        span:last-child {
          font-weight: 700;
          color: var(--lm-text);
        }
      }
    `,
  ],
})
export class DiarySnapshotPreviewComponent {
  @Input() snapshot: DiaryFinancialSnapshot | null = null;
}
