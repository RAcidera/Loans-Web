import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryEntry, DiarySearchFilters } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class SearchDiaryEntriesUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(filters: DiarySearchFilters): Observable<DiaryEntry[]> {
    return this.diaryRepository.searchEntries(filters);
  }
}
