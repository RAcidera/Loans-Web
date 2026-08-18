import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryEntry } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class GetDiaryEntryUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string): Observable<DiaryEntry> {
    return this.diaryRepository.getEntryById(diaryEntryId);
  }
}
