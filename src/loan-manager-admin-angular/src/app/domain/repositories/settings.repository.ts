import { Observable } from 'rxjs';
import { AppSettings } from '../entities/app-settings.entity';

/**
 * Port covering the General Settings area (currently just Business Time
 * Zone) — its own dedicated port, own lifecycle boundary, rather than
 * folded into UserRepository, mirroring how CashLedgerRepository/
 * ReportRepository each get their own port for their own boundary.
 */
export abstract class SettingsRepository {
  abstract getSettings(): Observable<AppSettings>;
  /** Admin only (enforced server-side) — the Settings page's General Settings card. */
  abstract updateBusinessTimeZone(timeZoneId: string): Observable<AppSettings>;
}
