import { Observable } from 'rxjs';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../entities/report.entity';

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
}
