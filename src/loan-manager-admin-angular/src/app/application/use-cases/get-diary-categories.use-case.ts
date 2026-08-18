import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DiaryCategory } from '../../domain/entities/diary-entry.entity';
import { DiaryRepository } from '../../domain/repositories/diary.repository';

@Injectable({ providedIn: 'root' })
export class GetDiaryCategoriesUseCase {
  constructor(private readonly diaryRepository: DiaryRepository) {}

  execute(): Observable<DiaryCategory[]> {
    return this.diaryRepository.getCategories();
  }
}
