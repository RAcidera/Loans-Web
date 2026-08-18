import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';
import { CalendarEventCardComponent } from '../calendar-event-card/calendar-event-card.component';
import { CalendarMoreEventsComponent } from '../calendar-more-events/calendar-more-events.component';

/** One Month/Week grid cell — requirements §Month Grid: 110–125px min height on desktop, muted adjacent-month dates, today subtly highlighted, event overflow capped via calendar-more-events rather than growing the row. */
@Component({
  selector: 'lm-calendar-day-cell',
  standalone: true,
  imports: [CommonModule, CalendarEventCardComponent, CalendarMoreEventsComponent],
  templateUrl: './calendar-day-cell.component.html',
  styleUrls: ['./calendar-day-cell.component.scss'],
})
export class CalendarDayCellComponent {
  @Input({ required: true }) dayNumber!: number;
  @Input() inCurrentMonth = true;
  @Input() isToday = false;
  @Input() tall = false;
  @Input() visibleEvents: CalendarEvent[] = [];
  @Input() overflowCount = 0;

  @Output() dayClick = new EventEmitter<void>();
  @Output() eventClick = new EventEmitter<CalendarEvent>();
}
