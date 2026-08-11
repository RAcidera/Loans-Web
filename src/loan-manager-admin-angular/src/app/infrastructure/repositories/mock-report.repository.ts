import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';

import { ReportRepository } from '../../domain/repositories/report.repository';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../../domain/entities/report.entity';

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
}
