import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { DiarySummary } from '../../domain/entities/diary-entry.entity';

/** Requirements diary-modern §5 — the five Summary Cards (Total Entries/This Month/Collections MTD/Loan Due/Reminders), matching the kpi-card visual pattern already used on Loans/Dashboard. All financial/count values come from the server (DiarySummaryDto), never computed here. */
@Component({
  selector: 'lm-diary-summary',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './diary-summary.component.html',
  styleUrls: ['./diary-summary.component.scss'],
})
export class DiarySummaryComponent {
  @Input() summary: DiarySummary | null = null;

  readonly currentMonthLabel = new Date().toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}
