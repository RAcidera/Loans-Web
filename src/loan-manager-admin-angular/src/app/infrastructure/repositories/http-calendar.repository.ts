import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CalendarRepository } from '../../domain/repositories/calendar.repository';
import { CalendarEvent, CalendarEventType } from '../../domain/entities/calendar-event.entity';

/** Talks to LoanManagementSystem.Api's CalendarController. Registered in app.config.ts in place of an abstract CalendarRepository. */
@Injectable()
export class HttpCalendarRepository extends CalendarRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getEvents(from: string, to: string, types?: CalendarEventType[]): Observable<CalendarEvent[]> {
    const params: Record<string, string> = { from, to };
    if (types?.length) params['types'] = types.join(',');
    return this.http.get<CalendarEvent[]>(`${this.baseUrl}/calendar/events`, { params });
  }
}
