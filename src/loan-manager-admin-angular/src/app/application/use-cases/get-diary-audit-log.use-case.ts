import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryAuditLogEntry } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class GetDiaryAuditLogUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string): Observable<DiaryAuditLogEntry[]> {
    return this.diaryRepository.getAuditLog(diaryEntryId);
  }
}
