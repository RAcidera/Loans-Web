import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

import { DiaryEntry } from '../../domain/entities/diary-entry.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { CategoryBadgeComponent } from '../category-badge/category-badge.component';
import { DiarySnapshotPreviewComponent } from '../diary-snapshot-preview/diary-snapshot-preview.component';
import { parseTags } from '../shared/tags-utils';
import { AppDatePipe } from '../shared/app-date.pipe';

/**
 * Requirements diary-modern §7's compact Entry Card — timeline dot/avatar,
 * time, category badge, title, notes preview, tags, and a contextual
 * business-information panel that varies by what the entry actually has
 * (§6): a compact Financial Snapshot preview when one was captured,
 * otherwise Customer/Loan context (with the linked loan's live due date
 * and balance) when the entry links to one. Plus a View button and a More
 * menu (§19: View/Edit/Delete plus Create Follow-up/Add Reminder/Duplicate).
 * One card per timeline row; diary-timeline owns grouping by date.
 */
@Component({
  selector: 'lm-diary-entry-card',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatMenuModule, CategoryBadgeComponent, DiarySnapshotPreviewComponent, AppDatePipe],
  templateUrl: './diary-entry-card.component.html',
  styleUrls: ['./diary-entry-card.component.scss'],
})
export class DiaryEntryCardComponent {
  @Input({ required: true }) entry!: DiaryEntry;
  /** The linked loan's current live data (due date, balance) — resolved by the caller (diary-list already loads the full loans list for its filter autocomplete), not fetched per-card. */
  @Input() loan?: Loan;

  @Output() view = new EventEmitter<DiaryEntry>();
  @Output() edit = new EventEmitter<DiaryEntry>();
  @Output() remove = new EventEmitter<DiaryEntry>();
  @Output() duplicate = new EventEmitter<DiaryEntry>();
  @Output() addReminder = new EventEmitter<DiaryEntry>();
  @Output() createFollowup = new EventEmitter<DiaryEntry>();

  get tags(): string[] {
    return parseTags(this.entry.tags);
  }

  get hasContextPanel(): boolean {
    return !!this.entry.snapshot || !!this.entry.customerName;
  }

  get avatarTint(): string {
    const hex = this.entry.categoryDisplayColor.startsWith('#') ? this.entry.categoryDisplayColor : `#${this.entry.categoryDisplayColor}`;
    return hex.length === 7 ? `${hex}1e` : hex;
  }

  get panelTint(): string {
    return this.avatarTint;
  }

  stop(event: Event): void {
    event.stopPropagation();
  }
}
