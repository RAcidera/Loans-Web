import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryFinancialSnapshot } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class GetDiarySnapshotUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string): Observable<DiaryFinancialSnapshot> {
    return this.diaryRepository.getSnapshot(diaryEntryId);
  }
}
