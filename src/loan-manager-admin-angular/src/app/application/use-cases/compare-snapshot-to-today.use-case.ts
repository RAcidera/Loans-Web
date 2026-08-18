import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FinancialComparison } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class CompareSnapshotToTodayUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string): Observable<FinancialComparison> {
    return this.diaryRepository.compareToToday(diaryEntryId);
  }
}
