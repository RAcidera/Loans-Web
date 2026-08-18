import { Observable } from 'rxjs';
import { CalendarEvent, CalendarEventType } from '../entities/calendar-event.entity';

/** Port covering the Calendar module (requirements §18-19) — its own lifecycle boundary, separate from Diary/Loans. */
export abstract class CalendarRepository {
  /** Requirements §18/§19/§22 — events across [from, to] (both "yyyy-MM-dd"), optionally restricted to a subset of event types. */
  abstract getEvents(from: string, to: string, types?: CalendarEventType[]): Observable<CalendarEvent[]>;
}
