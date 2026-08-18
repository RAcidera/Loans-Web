import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

/** Requirements diary-modern §3 — page header with the "+ New Entry" split action (Diary Entry / Follow-up-Reminder / Promise to Pay), mirroring the Calendar toolbar's own "+ New" menu for a consistent pattern across modules. */
@Component({
  selector: 'lm-diary-header',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './diary-header.component.html',
  styleUrls: ['./diary-header.component.scss'],
})
export class DiaryHeaderComponent {
  @Output() newDiaryEntry = new EventEmitter<void>();
  @Output() newReminder = new EventEmitter<void>();
  @Output() newPromise = new EventEmitter<void>();
}
