import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarEvent, CalendarEventType } from '../../domain/entities/calendar-event.entity';
import { CalendarRepository } from '../../domain/repositories/calendar.repository';

@Injectable({ providedIn: 'root' })
export class GetCalendarEventsUseCase {
  constructor(private readonly calendarRepository: CalendarRepository) {}

  execute(from: string, to: string, types?: CalendarEventType[]): Observable<CalendarEvent[]> {
    return this.calendarRepository.getEvents(from, to, types);
  }
}
