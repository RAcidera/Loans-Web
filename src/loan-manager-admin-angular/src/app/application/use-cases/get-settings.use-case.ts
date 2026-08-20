import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppSettings } from '../../domain/entities/app-settings.entity';
import { SettingsRepository } from '../../domain/repositories/settings.repository';

@Injectable({ providedIn: 'root' })
export class GetSettingsUseCase {
  constructor(private readonly settingsRepository: SettingsRepository) {}

  execute(): Observable<AppSettings> {
    return this.settingsRepository.getSettings();
  }
}
