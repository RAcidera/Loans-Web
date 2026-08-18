import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../domain/entities/calendar-event.entity';
import { CalendarDayCellData } from '../shared/calendar-day-cell.model';
import { CalendarDayCellComponent } from '../calendar-day-cell/calendar-day-cell.component';

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

/** Requirements' Month Grid — fixed 6-row (42-cell) 7-column layout, large calendar space per the layout priority ("Large calendar grid"). */
@Component({
  selector: 'lm-calendar-month-view',
  standalone: true,
  imports: [CommonModule, CalendarDayCellComponent],
  templateUrl: './calendar-month-view.component.html',
  styleUrls: ['./calendar-month-view.component.scss'],
})
export class CalendarMonthViewComponent {
  @Input({ required: true }) weeks: CalendarDayCellData[][] = [];
  @Output() dayClick = new EventEmitter<CalendarDayCellData>();
  @Output() eventClick = new EventEmitter<CalendarEvent>();

  weekdayLabels = WEEKDAY_LABELS;
}
