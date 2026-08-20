import { Injectable, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { SettingsRepository } from '../domain/repositories/settings.repository';

const DEFAULT_TIME_ZONE_ID = 'Asia/Manila';

/**
 * The single client-side source of "what timezone is the business in" and
 * "what is today, in business-local terms" — backed by GET /api/settings,
 * not the browser's own clock/timezone. Components that need to group or
 * highlight things by "today" (calendar, diary) should read `today()`
 * instead of `new Date()`; the two shared date pipes (`appDateTime`,
 * `appDate`) read `businessTimeZoneId()` internally so most templates never
 * need to touch this service directly.
 *
 * Fetches once, on construction (effectively app startup, since this is a
 * root-provided singleton first injected very early). Deliberately doesn't
 * block app bootstrap on the fetch (e.g. via an APP_INITIALIZER) — the
 * login page has no auth token yet, so that request would 401 there, and
 * failing open to the hardcoded default is the right behavior rather than
 * stalling the whole app. `refresh()` is called again by AuthService right
 * after a successful login, so the real configured value is loaded as soon
 * as a token exists.
 */
@Injectable({ providedIn: 'root' })
export class BusinessTimeService {
  readonly businessTimeZoneId = signal<string>(DEFAULT_TIME_ZONE_ID);
  /** "yyyy-MM-dd", business-local — undefined until the first successful fetch resolves. */
  readonly today = signal<string | undefined>(undefined);

  constructor(private readonly settingsRepository: SettingsRepository) {
    this.refresh();
  }

  refresh(): void {
    this.settingsRepository
      .getSettings()
      .pipe(catchError(() => of(null)))
      .subscribe((settings) => {
        if (!settings) return;
        this.businessTimeZoneId.set(settings.businessTimeZoneId);
        this.today.set(settings.currentBusinessDate);
      });
  }
}
