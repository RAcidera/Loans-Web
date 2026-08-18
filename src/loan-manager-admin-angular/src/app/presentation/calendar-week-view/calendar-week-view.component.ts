import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';
import { CalendarDayCellData } from '../shared/calendar-day-cell.model';
import { CalendarDayCellComponent } from '../calendar-day-cell/calendar-day-cell.component';

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

/** Requirements' Week view — a single, taller row of the same 7 day-cells the Month grid uses, so more events fit per day before overflowing. */
@Component({
  selector: 'lm-calendar-week-view',
  standalone: true,
  imports: [CommonModule, CalendarDayCellComponent],
  templateUrl: './calendar-week-view.component.html',
  styleUrls: ['./calendar-week-view.component.scss'],
})
export class CalendarWeekViewComponent {
  @Input({ required: true }) days: CalendarDayCellData[] = [];
  @Output() dayClick = new EventEmitter<CalendarDayCellData>();
  @Output() eventClick = new EventEmitter<CalendarEvent>();

  weekdayLabels = WEEKDAY_LABELS;
}
