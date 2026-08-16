import { Observable } from 'rxjs';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../entities/report.entity';
import {
  InterestEarnedFilters,
  InterestEarnedLoanBreakdown,
  InterestEarnedMonthlyPoint,
  InterestEarnedOverview,
  InterestEarnedRow,
} from '../entities/interest-earned-report.entity';
import { PagedResult } from '../entities/paged-result.entity';

/**
 * Port covering SRS 3.5's Reports page — read-only aggregate views over
 * Loans and Customers. Kept separate from LoanRepository (per-loan
 * operations) and CashLedgerRepository (funds tracking): this is
 * cross-cutting reporting, not either of those concerns.
 */
export abstract class ReportRepository {
  abstract getInterestSummary(): Observable<InterestSummary>;
  abstract getCustomerSummary(): Observable<CustomerSummary[]>;
  abstract getPeriodSummary(startDate: string, endDate: string): Observable<PeriodSummary>;
  /** Downloads the selected period's loans as a CSV file (SRS 3.5 "Export"). */
  abstract exportPeriodReportCsv(startDate: string, endDate: string): Observable<Blob>;

  abstract getInterestEarnedPage(
    pageIndex: number, pageSize: number, filters: InterestEarnedFilters, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<InterestEarnedRow>>;
  abstract getInterestEarnedOverview(filters: InterestEarnedFilters): Observable<InterestEarnedOverview>;
  abstract getInterestEarnedMonthlyChart(filters: InterestEarnedFilters): Observable<InterestEarnedMonthlyPoint[]>;
  abstract getInterestEarnedLoanBreakdown(loanId: string, fromDate: string, toDate: string): Observable<InterestEarnedLoanBreakdown>;
  abstract exportInterestEarnedXlsx(filters: InterestEarnedFilters): Observable<Blob>;
  abstract exportInterestEarnedPdf(filters: InterestEarnedFilters): Observable<Blob>;
}
