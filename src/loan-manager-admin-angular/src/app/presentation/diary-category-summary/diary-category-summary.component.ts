import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { DiaryCategoryCount } from '../../domain/entities/diary-entry.entity';

/** Requirements diary-modern §20's Category Summary — all-time counts per category (server-computed, DiarySummaryDto.categoryCounts), clicking a row filters the timeline. */
@Component({
  selector: 'lm-diary-category-summary',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './diary-category-summary.component.html',
  styleUrls: ['./diary-category-summary.component.scss'],
})
export class DiaryCategorySummaryComponent {
  @Input() categories: DiaryCategoryCount[] = [];
  @Input() totalEntries = 0;
  @Input() selectedCategoryId: string | null | undefined = null;

  @Output() categorySelected = new EventEmitter<string | null>();
}
