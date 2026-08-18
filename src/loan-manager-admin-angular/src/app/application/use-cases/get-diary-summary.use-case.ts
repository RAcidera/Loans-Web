import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiarySummary } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class GetDiarySummaryUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(): Observable<DiarySummary> {
    return this.diaryRepository.getSummary();
  }
}
