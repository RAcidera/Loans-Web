// Domain layer — the Dashboard's "Monthly Capital Growth" panel, mirroring
// GET /api/dashboard/monthly-capital-growth field-for-field. Every amount
// is nullable: a month before recorded history is "unavailable", never 0.

/** complete | partialmonth (current month, as of today) | partialhistory (records begin mid-month) | nohistory (amounts unavailable). */
export type CapitalGrowthMonthStatus = 'complete' | 'partialmonth' | 'partialhistory' | 'nohistory';

export type CapitalGrowthRange = 6 | 12 | 24;

export interface CapitalGrowthMonth {
  /** yyyy-MM, business-local. */
  monthKey: string;
  startDate: string;
  endCutoffDate: string;
  isPartialMonth: boolean;
  status: CapitalGrowthMonthStatus;
  beginningCash: number | null;
  endingCash: number | null;
  beginningGrossReceivables: number | null;
  endingGrossReceivables: number | null;
  beginningPosition: number | null;
  endingPosition: number | null;
  ownerWithdrawals: number | null;
  ownerDeposits: number | null;
  positionChange: number | null;
  adjustedGrowth: number | null;
  /** Null unless the beginning position is positive. */
  growthPercent: number | null;
}

export interface CapitalGrowthSummary {
  /** Completed months plus the current month to date. */
  totalGrowth: number | null;
  monthsInTotal: number;
  averageCompletedMonthGrowth: number | null;
  completedMonthCount: number;
  averageGrowthIncludingCurrentMonth: number | null;
  /** Completed months only — the current partial month never wins. */
  bestMonthKey: string | null;
  bestMonthGrowth: number | null;
  /** Null unless every month of the prior equal-length window has full history. */
  previousPeriodGrowth: number | null;
  changeVsPreviousPeriodPercent: number | null;
}

export interface SnapshotReconciliation {
  snapshotDateTimeUtc: string;
  businessDate: string;
  snapshotPosition: number;
  reconstructedPosition: number;
  difference: number;
}

export interface CapitalGrowthReconciliation {
  dashboardPosition: number;
  currentEndingPosition: number | null;
  difference: number | null;
  matches: boolean;
  snapshots: SnapshotReconciliation[];
}

export interface CapitalGrowthWarning {
  code: string;
  message: string;
  loanNumbers: string[];
}

export interface MonthlyCapitalGrowth {
  timeZone: string;
  asOfUtc: string;
  asOfDate: string;
  currency: string;
  months: number;
  /** Always "restated" — past months are recomputed from today's ledger records, not replayed audit-exact. */
  historyBasis: string;
  historyStartDate: string | null;
  series: CapitalGrowthMonth[];
  summary: CapitalGrowthSummary;
  reconciliation: CapitalGrowthReconciliation;
  warnings: CapitalGrowthWarning[];
}
