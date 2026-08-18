import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DiarySummary } from '../../domain/entities/diary-entry.entity';

/** Requirements diary-modern §20's Quick Summary — Collections/Loans Released Today+MTD, the same server-computed figures as DiarySummaryComponent's cards (DiarySummaryDto), reconciling with the Dashboard by construction since both ultimately read IFinancialSnapshotService. */
@Component({
  selector: 'lm-diary-quick-summary',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './diary-quick-summary.component.html',
  styleUrls: ['./diary-quick-summary.component.scss'],
})
export class DiaryQuickSummaryComponent {
  @Input() summary: DiarySummary | null = null;

  constructor(private readonly router: Router) {}

  viewReport(): void {
    this.router.navigate(['/reports']);
  }
}
