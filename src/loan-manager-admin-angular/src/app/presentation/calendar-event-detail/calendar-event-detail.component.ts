import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';

/**
 * Requirements' calendar-event-detail component — a quick-preview dialog
 * shown when an event card is clicked, before navigating away. Its primary
 * action follows the requirements' Event Click table (Loan Due → Loan
 * Detail, Diary → Diary Detail, etc.), resolved by calendar-page (the only
 * place that knows the router) rather than here.
 */
@Component({
  selector: 'lm-calendar-event-detail',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './calendar-event-detail.component.html',
  styleUrls: ['./calendar-event-detail.component.scss'],
})
export class CalendarEventDetailComponent {
  readonly event: CalendarEvent = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CalendarEventDetailComponent, boolean>);

  get formattedDate(): string {
    return new Date(`${this.event.date}T00:00:00`).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
  }

  get formattedTime(): string | null {
    if (!this.event.time) return null;
    const [h, m] = this.event.time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 === 0 ? 12 : h % 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${period}`;
  }

  get hasDestination(): boolean {
    return !!this.event.linkedEntityId;
  }

  view(): void {
    this.dialogRef.close(true);
  }

  close(): void {
    this.dialogRef.close(false);
  }
}
