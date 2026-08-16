import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';

import { ReportRepository } from '../../domain/repositories/report.repository';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../../domain/entities/report.entity';
import {
  InterestEarnedFilters,
  InterestEarnedLoanBreakdown,
  InterestEarnedMonthlyPoint,
  InterestEarnedOverview,
  InterestEarnedRow,
} from '../../domain/entities/interest-earned-report.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';

const MOCK_ROWS: InterestEarnedRow[] = [
  { loanId: 'L-2034', loanNumber: 'LOA00009', customerId: 'C-005', customerName: 'Ana Villanueva', loanDate: '2026-08-10', dueDate: '2026-10-09', principal: 10000, contractInterest: 300, extensionInterest: 150, earnedBeforePeriod: 200, earnedThisPeriod: 150, totalEarned: 350, adjustment: 0, finalEarned: 350, status: 'active', classification: 'normal' },
  { loanId: 'L-2035', loanNumber: 'LOA00008', customerId: 'C-005', customerName: 'Ana Villanueva', loanDate: '2026-08-09', dueDate: '2026-10-08', principal: 1000, contractInterest: 30, extensionInterest: 0, earnedBeforePeriod: 20, earnedThisPeriod: 10, totalEarned: 30, adjustment: 0, finalEarned: 30, status: 'paid', classification: 'normal' },
  { loanId: 'L-2036', loanNumber: 'LOA00005', customerId: 'C-001', customerName: 'Maria Santos', loanDate: '2026-07-01', dueDate: '2026-08-29', principal: 3500, contractInterest: 210, extensionInterest: 0, earnedBeforePeriod: 120, earnedThisPeriod: 70, totalEarned: 190, adjustment: -20, finalEarned: 170, status: 'overdue', classification: 'watchlist' },
];

// Illustrative figures only — not derived from MockLoanRepository's
// fixtures, same cross-boundary tradeoff as mock-cash-ledger.repository.ts.
const CUSTOMER_SUMMARIES: CustomerSummary[] = [
  { customerId: 'C-001', customerName: 'Maria Santos', totalBorrowed: 8500, totalPaid: 2500, loansCount: 2 },
  { customerId: 'C-002', customerName: 'Jun Dela Cruz', totalBorrowed: 3000, totalPaid: 3090, loansCount: 1 },
  { customerId: 'C-003', customerName: 'Liza Ramos', totalBorrowed: 2000, totalPaid: 0, loansCount: 1 },
  { customerId: 'C-004', customerName: 'Ronnie Bautista', totalBorrowed: 4000, totalPaid: 1000, loansCount: 1 },
  { customerId: 'C-005', customerName: 'Ana Villanueva', totalBorrowed: 1000, totalPaid: 400, loansCount: 1 },
];

/** Stand-in data source implementing ReportRepository, for offline demo mode. */
@Injectable()
export class MockReportRepository extends ReportRepository {
  getInterestSummary(): Observable<InterestSummary> {
    return of({ totalInterestEarned: 555 }).pipe(delay(150));
  }

  getCustomerSummary(): Observable<CustomerSummary[]> {
    return of(CUSTOMER_SUMMARIES).pipe(delay(150));
  }

  getPeriodSummary(startDate: string, endDate: string): Observable<PeriodSummary> {
    return of({
      startDate,
      endDate,
      loansOriginated: 3,
      paymentsCollected: 4045,
      extensionsGranted: 1,
      interestEarned: 315,
    }).pipe(delay(150));
  }

  exportPeriodReportCsv(): Observable<Blob> {
    const csv = 'Loan ID,Customer,Principal,Start Date,Due Date,Status,Total Interest,Total Paid,Balance\n';
    return of(new Blob([csv], { type: 'text/csv' })).pipe(delay(150));
  }

  private filterRows(filters: InterestEarnedFilters): InterestEarnedRow[] {
    return MOCK_ROWS.filter((r) => r.loanDate <= filters.toDate && r.dueDate >= filters.fromDate)
      .filter((r) => !filters.status || r.status === filters.status)
      .filter((r) => !filters.classification || r.classification === filters.classification)
      .filter((r) => !filters.search || r.loanNumber.toLowerCase().includes(filters.search.toLowerCase()) || r.customerName.toLowerCase().includes(filters.search.toLowerCase()));
  }

  getInterestEarnedPage(pageIndex: number, pageSize: number, filters: InterestEarnedFilters): Observable<PagedResult<InterestEarnedRow>> {
    const rows = this.filterRows(filters);
    const items = rows.slice(pageIndex * pageSize, pageIndex * pageSize + pageSize);
    return of({ items, totalCount: rows.length }).pipe(delay(150));
  }

  getInterestEarnedOverview(filters: InterestEarnedFilters): Observable<InterestEarnedOverview> {
    const rows = this.filterRows(filters);
    const sum = (selector: (r: InterestEarnedRow) => number) => rows.reduce((s, r) => s + selector(r), 0);
    return of({
      summary: {
        totalEarnedInterest: sum((r) => r.earnedThisPeriod) + sum((r) => r.adjustment),
        originalInterestEarned: sum((r) => r.earnedThisPeriod),
        extensionInterestEarned: 0,
        interestAdjustments: sum((r) => r.adjustment),
        interestCollected: null,
        interestReceivable: null,
      },
      totals: {
        principal: sum((r) => r.principal),
        contractInterest: sum((r) => r.contractInterest),
        extensionInterest: sum((r) => r.extensionInterest),
        earnedBeforePeriod: sum((r) => r.earnedBeforePeriod),
        earnedThisPeriod: sum((r) => r.earnedThisPeriod),
        totalEarned: sum((r) => r.totalEarned),
        adjustment: sum((r) => r.adjustment),
        finalEarned: sum((r) => r.finalEarned),
        count: rows.length,
      },
    }).pipe(delay(150));
  }

  getInterestEarnedMonthlyChart(): Observable<InterestEarnedMonthlyPoint[]> {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return of(months.map((month, i) => ({ month, currentYear: 200 + i * 30, previousYear: 150 + i * 22 }))).pipe(delay(150));
  }

  getInterestEarnedLoanBreakdown(loanId: string, fromDate: string, toDate: string): Observable<InterestEarnedLoanBreakdown> {
    const row = MOCK_ROWS.find((r) => r.loanId === loanId) ?? MOCK_ROWS[0];
    return of({
      loanId: row.loanId,
      loanNumber: row.loanNumber,
      customerName: row.customerName,
      originalContractInterest: row.contractInterest,
      adjustedContractInterest: row.contractInterest + row.adjustment,
      interestAdjustment: row.adjustment,
      periods: [
        {
          label: 'Original Loan', periodStart: row.loanDate, periodEndInclusive: row.dueDate, termDays: 60,
          contractAmount: row.contractInterest, dailyInterest: row.contractInterest / 60, earnedDaysThisPeriod: 22,
          earnedBeforePeriod: row.earnedBeforePeriod, earnedThisPeriod: row.earnedThisPeriod, totalEarned: row.totalEarned,
        },
      ],
    }).pipe(delay(150));
  }

  exportInterestEarnedXlsx(): Observable<Blob> {
    return of(new Blob([''], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })).pipe(delay(150));
  }

  exportInterestEarnedPdf(): Observable<Blob> {
    return of(new Blob([''], { type: 'application/pdf' })).pipe(delay(150));
  }
}
