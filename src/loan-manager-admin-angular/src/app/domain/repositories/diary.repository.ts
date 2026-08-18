import { Observable } from 'rxjs';
import {
  DiaryAuditLogEntry,
  DiaryCategory,
  DiaryEntry,
  DiaryFinancialSnapshot,
  DiarySearchFilters,
  DiarySummary,
  FinancialComparison,
} from '../entities/diary-entry.entity';

/** Requirements §6 create fields — CreatedBy is not here, the backend derives it from the authenticated user. */
export interface CreateDiaryEntryFields {
  title: string;
  categoryId: string;
  notes: string;
  captureFinancialSnapshot: boolean;
  customerId?: string;
  loanId?: string;
  entryDate?: string;
  entryTime?: string;
  reminderDate?: string;
  reminderTime?: string;
  tags?: string;
}

/** Requirements §13's editable fields — never the financial snapshot (requirements §10). */
export interface UpdateDiaryEntryFields {
  title: string;
  categoryId: string;
  notes: string;
  customerId?: string;
  loanId?: string;
  entryDate?: string;
  entryTime?: string;
  reminderDate?: string;
  reminderTime?: string;
  tags?: string;
}

/**
 * Port covering the Diary/Journal module — its own lifecycle boundary,
 * separate from LoanRepository/CashLedgerRepository, per CLAUDE.md's "one
 * repository port per lifecycle boundary" rule.
 */
export abstract class DiaryRepository {
  /** Active categories, ordered by SortOrder — requirements §5/§6's category dropdown. */
  abstract getCategories(): Observable<DiaryCategory[]>;

  /** The Diary timeline (requirements §11/§12) — sorted EntryDateTime DESC, not paged. */
  abstract searchEntries(filters: DiarySearchFilters): Observable<DiaryEntry[]>;

  abstract getEntryById(diaryEntryId: string): Observable<DiaryEntry>;

  /** Requirements §6/§7 — the server computes and stores the financial snapshot when captureFinancialSnapshot is true; never accepts Angular-calculated totals. */
  abstract createEntry(fields: CreateDiaryEntryFields): Observable<DiaryEntry>;

  abstract updateEntry(diaryEntryId: string, fields: UpdateDiaryEntryFields): Observable<DiaryEntry>;

  abstract deleteEntry(diaryEntryId: string): Observable<void>;

  abstract getSnapshot(diaryEntryId: string): Observable<DiaryFinancialSnapshot>;

  /** Requirements §15 "Compare to Today". */
  abstract compareToToday(diaryEntryId: string): Observable<FinancialComparison>;

  abstract getAuditLog(diaryEntryId: string): Observable<DiaryAuditLogEntry[]>;

  /** Requirements diary-modern §5/§20 — the Summary Cards and right-sidebar Quick Summary/Category Summary. */
  abstract getSummary(): Observable<DiarySummary>;
}
