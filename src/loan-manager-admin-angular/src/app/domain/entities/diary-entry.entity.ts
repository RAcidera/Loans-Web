// Domain layer — mirrors the Diary/Financial-Snapshot/Compare-to-Today
// module (requirements doc: diary-calendar-financial-snapshot-requirements.md).

export interface DiaryCategory {
  categoryId: string;
  name: string;
  icon: string;
  displayColor: string;
  isActive: boolean;
  sortOrder: number;
}

/** Requirements §8 — immutable once captured; never recalculated client-side (requirements §7/§10). */
export interface DiaryFinancialSnapshot {
  snapshotId: string;
  diaryEntryId: string;
  grossReceivables: number;
  collectibleReceivables: number;
  badLoanReceivables: number;
  cashOnHand: number;
  activeLoanCount: number;
  overdueLoanCount: number;
  badLoanCount: number;
  collectionsToday: number;
  collectionsMonthToDate: number;
  loanReleasesToday: number;
  loanReleasesMonthToDate: number;
  snapshotDateTime: string;
}

/** Requirements §4/§13. Category display fields are denormalized onto the entry so the timeline never needs a second lookup — see MappingExtensions.ToDto on the backend. */
export interface DiaryEntry {
  diaryEntryId: string;
  entryDate: string;
  entryTime: string;
  entryDateTime: string;
  title: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryDisplayColor: string;
  notes: string;
  /** Comma-separated on the wire (requirements §9's optional Tags field) — components split/join on "," rather than the backend storing a child collection. */
  tags: string;
  customerId?: string;
  customerName?: string;
  loanId?: string;
  loanNumber?: string;
  reminderDate?: string;
  reminderTime?: string;
  snapshot?: DiaryFinancialSnapshot;
  createdBy: string;
  createdAt: string;
  modifiedBy: string;
  modifiedAt: string;
}

/** Requirements §12 — the Diary timeline's filter bar. */
export interface DiarySearchFilters {
  search?: string;
  categoryId?: string;
  dateFrom?: string;
  dateTo?: string;
  customerId?: string;
  loanId?: string;
  hasFinancialSnapshot?: boolean;
  hasReminder?: boolean;
}

/**
 * One row of the Financial Comparison table (requirements §15-17). Key is a
 * stable identifier ("badLoanReceivables", "cashOnHand", ...) the
 * comparison-metric component matches against to apply the requirement's
 * contextual coloring rules — see financial-comparison.component.ts.
 * percentChange is null when snapshotValue was zero (requirements §16 —
 * rendered as "N/A"/"New" depending on currentValue).
 */
export interface FinancialComparisonMetric {
  key: string;
  label: string;
  snapshotValue: number;
  currentValue: number;
  change: number;
  percentChange: number | null;
}

export interface FinancialComparison {
  snapshotDate: string;
  todayDate: string;
  metrics: FinancialComparisonMetric[];
}

/** Backs the Diary Entry Detail "Audit Information" section (requirements §13/§24). */
export interface DiaryAuditLogEntry {
  auditLogId: string;
  diaryEntryId: string;
  action: string;
  description: string;
  performedBy: string;
  occurredAt: string;
}

/** One row of the sidebar's Category Summary (requirements diary-modern §20) — count is all-time, independent of the timeline's current filters. */
export interface DiaryCategoryCount {
  categoryId: string;
  name: string;
  icon: string;
  displayColor: string;
  count: number;
}

/** Backs the page header's Summary Cards and the right sidebar's Quick Summary/Category Summary (requirements diary-modern §5/§20) — every financial figure is server-computed, never derived from other Angular state. */
export interface DiarySummary {
  totalEntries: number;
  entriesThisMonth: number;
  loanDueCount: number;
  pendingRemindersCount: number;
  collectionsToday: number;
  collectionsMonthToDate: number;
  loanReleasesToday: number;
  loanReleasesMonthToDate: number;
  categoryCounts: DiaryCategoryCount[];
}
