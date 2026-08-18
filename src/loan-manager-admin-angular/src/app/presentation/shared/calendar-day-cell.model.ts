import { CalendarEvent } from '../../domain/entities/calendar-event.entity';

/** One rendered grid cell — shared shape between calendar-page (which builds it) and calendar-month-view/calendar-week-view/calendar-day-cell (which render it). */
export interface CalendarDayCellData {
  date: Date;
  dateKey: string;
  dayNumber: number;
  inCurrentMonth: boolean;
  isToday: boolean;
  visibleEvents: CalendarEvent[];
  overflowCount: number;
}
