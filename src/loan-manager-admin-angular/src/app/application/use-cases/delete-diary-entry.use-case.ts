import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class DeleteDiaryEntryUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(diaryEntryId: string): Observable<void> {
    return this.diaryRepository.deleteEntry(diaryEntryId);
  }
}
