import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

export type CalendarViewMode = 'month' | 'week' | 'day';

/** Requirements' "single compact toolbar" — Today/Prev/Next/period on the left, Month/Week/Day + "+ New" on the right. No separate oversized navigation card. */
@Component({
  selector: 'lm-calendar-toolbar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatButtonToggleModule, MatIconModule, MatMenuModule],
  templateUrl: './calendar-toolbar.component.html',
  styleUrls: ['./calendar-toolbar.component.scss'],
})
export class CalendarToolbarComponent {
  @Input({ required: true }) viewMode!: CalendarViewMode;
  @Input({ required: true }) periodLabel!: string;

  @Output() today = new EventEmitter<void>();
  @Output() prev = new EventEmitter<void>();
  @Output() next = new EventEmitter<void>();
  @Output() viewModeChange = new EventEmitter<CalendarViewMode>();
  @Output() newDiaryEntry = new EventEmitter<void>();
  @Output() newReminder = new EventEmitter<void>();
  @Output() newPromise = new EventEmitter<void>();
}
