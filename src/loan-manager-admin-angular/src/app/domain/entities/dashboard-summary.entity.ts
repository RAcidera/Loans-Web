// Domain layer — everything on the Dashboard beyond Cash on Hand
// (CashSummary) and Recent Payments (PaymentWithCustomer), which already
// had their own entities/endpoints.

import { Loan } from './loan.entity';

/** One calendar month's collected-payments total, this year vs. the same month last year — "Collections Overview" bar chart. */
export interface MonthlyCollection {
  month: string;
  thisYear: number;
  lastYear: number;
}

/** One day's collected-payments total — "Collections (Past 7 Days)" line chart. */
export interface DailyCollection {
  date: string;
  amount: number;
}

/** Every non-written-off loan's TotalAmountDue, partitioned by current disposition — "Receivables Breakdown" donut chart. */
export interface ReceivablesBreakdown {
  current: number;
  overdue: number;
  badLoan: number;
  paid: number;
}

export interface DashboardSummary {
  grossReceivables: number;
  /** Null when there's nothing a month old to compare against (e.g. a brand-new portfolio). */
  grossReceivablesChangePercent: number | null;
  collectibleReceivables: number;
  collectibleReceivablesChangePercent: number | null;
  badLoanReceivables: number;
  badLoanReceivablesChangePercent: number | null;
  activeLoansCount: number;
  activeLoansChangePercent: number | null;
  overdueLoansCount: number;
  overdueLoansChangePercent: number | null;
  loansDueThisWeekCount: number;
  monthlyCollections: MonthlyCollection[];
  last7DaysCollections: DailyCollection[];
  receivablesBreakdown: ReceivablesBreakdown;
  recentLoans: Loan[];
}
