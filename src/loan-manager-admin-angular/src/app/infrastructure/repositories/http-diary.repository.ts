import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateDiaryEntryFields,
  DiaryRepository,
  UpdateDiaryEntryFields,
} from '../../domain/repositories/diary.repository';
import {
  DiaryAuditLogEntry,
  DiaryCategory,
  DiaryEntry,
  DiaryFinancialSnapshot,
  DiarySearchFilters,
  DiarySummary,
  FinancialComparison,
} from '../../domain/entities/diary-entry.entity';

/** Flattens DiarySearchFilters into HttpClient query params, matching DiaryController.Search's query-string names. */
function toFilterParams(filters: DiarySearchFilters): Record<string, string | boolean> {
  const params: Record<string, string | boolean> = {};
  if (filters.search) params['search'] = filters.search;
  if (filters.categoryId) params['categoryId'] = filters.categoryId;
  if (filters.dateFrom) params['dateFrom'] = filters.dateFrom;
  if (filters.dateTo) params['dateTo'] = filters.dateTo;
  if (filters.customerId) params['customerId'] = filters.customerId;
  if (filters.loanId) params['loanId'] = filters.loanId;
  if (filters.hasFinancialSnapshot !== undefined) params['hasSnapshot'] = filters.hasFinancialSnapshot;
  if (filters.hasReminder !== undefined) params['hasReminder'] = filters.hasReminder;
  return params;
}

/** Talks to LoanManagementSystem.Api's DiaryController/DiaryCategoriesController. Registered in app.config.ts in place of an abstract DiaryRepository. */
@Injectable()
export class HttpDiaryRepository extends DiaryRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getCategories(): Observable<DiaryCategory[]> {
    return this.http.get<DiaryCategory[]>(`${this.baseUrl}/diary-categories`);
  }

  searchEntries(filters: DiarySearchFilters): Observable<DiaryEntry[]> {
    return this.http.get<DiaryEntry[]>(`${this.baseUrl}/diary`, { params: toFilterParams(filters) });
  }

  getEntryById(diaryEntryId: string): Observable<DiaryEntry> {
    return this.http.get<DiaryEntry>(`${this.baseUrl}/diary/${diaryEntryId}`);
  }

  createEntry(fields: CreateDiaryEntryFields): Observable<DiaryEntry> {
    return this.http.post<DiaryEntry>(`${this.baseUrl}/diary`, fields);
  }

  updateEntry(diaryEntryId: string, fields: UpdateDiaryEntryFields): Observable<DiaryEntry> {
    return this.http.put<DiaryEntry>(`${this.baseUrl}/diary/${diaryEntryId}`, fields);
  }

  deleteEntry(diaryEntryId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/diary/${diaryEntryId}`);
  }

  getSnapshot(diaryEntryId: string): Observable<DiaryFinancialSnapshot> {
    return this.http.get<DiaryFinancialSnapshot>(`${this.baseUrl}/diary/${diaryEntryId}/snapshot`);
  }

  compareToToday(diaryEntryId: string): Observable<FinancialComparison> {
    return this.http.get<FinancialComparison>(`${this.baseUrl}/diary/${diaryEntryId}/compare-to-today`);
  }

  getAuditLog(diaryEntryId: string): Observable<DiaryAuditLogEntry[]> {
    return this.http.get<DiaryAuditLogEntry[]>(`${this.baseUrl}/diary/${diaryEntryId}/audit-log`);
  }

  getSummary(): Observable<DiarySummary> {
    return this.http.get<DiarySummary>(`${this.baseUrl}/diary/summary`);
  }
}
