import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryEntry } from '../../domain/entities/diary-entry.entity';
import { CreateDiaryEntryFields, DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class CreateDiaryEntryUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(fields: CreateDiaryEntryFields): Observable<DiaryEntry> {
    return this.diaryRepository.createEntry(fields);
  }
}
