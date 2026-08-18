import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';
import { CalendarEventCardComponent } from '../calendar-event-card/calendar-event-card.component';

/** Requirements' Day view — a full-width list of that day's structured event cards (compact=false, so no ellipsis truncation), sorted by time. Also the destination for a Month/Week cell's "+N more" click. */
@Component({
  selector: 'lm-calendar-day-view',
  standalone: true,
  imports: [CommonModule, CalendarEventCardComponent],
  templateUrl: './calendar-day-view.component.html',
  styleUrls: ['./calendar-day-view.component.scss'],
})
export class CalendarDayViewComponent {
  @Input() events: CalendarEvent[] = [];
  @Output() eventClick = new EventEmitter<CalendarEvent>();
}
