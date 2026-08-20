import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppSettings } from '../../domain/entities/app-settings.entity';
import { SettingsRepository } from '../../domain/repositories/settings.repository';

@Injectable({ providedIn: 'root' })
export class UpdateBusinessTimeZoneUseCase {
  constructor(private readonly settingsRepository: SettingsRepository) {}

  execute(timeZoneId: string): Observable<AppSettings> {
    return this.settingsRepository.updateBusinessTimeZone(timeZoneId);
  }
}
