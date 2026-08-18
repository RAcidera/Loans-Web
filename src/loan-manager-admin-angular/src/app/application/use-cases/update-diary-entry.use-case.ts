import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryEntry } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository, UpdateDiaryEntryFields } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class UpdateDiaryEntryUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string, fields: UpdateDiaryEntryFields): Observable<DiaryEntry> {
    return this.diaryRepository.updateEntry(diaryEntryId, fields);
  }
}
