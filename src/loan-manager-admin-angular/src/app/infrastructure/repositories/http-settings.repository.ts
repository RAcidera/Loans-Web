import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SettingsRepository } from '../../domain/repositories/settings.repository';
import { AppSettings } from '../../domain/entities/app-settings.entity';

/** Talks to SettingsController on the .NET backend. Registered in app.config.ts in place of the abstract SettingsRepository port. */
@Injectable()
export class HttpSettingsRepository extends SettingsRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getSettings(): Observable<AppSettings> {
    return this.http.get<AppSettings>(`${this.baseUrl}/settings`);
  }

  updateBusinessTimeZone(timeZoneId: string): Observable<AppSettings> {
    return this.http.put<AppSettings>(`${this.baseUrl}/settings/business-time-zone`, { timeZoneId });
  }
}
