import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestEarnedLoanBreakdown } from '../../domain/entities/interest-earned-report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

/** The Loan Interest Drill-Down dialog — how the system arrived at a loan's earned-interest figure. */
@Injectable({ providedIn: 'root' })
export class GetInterestEarnedLoanBreakdownUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(loanId: string, fromDate: string, toDate: string): Observable<InterestEarnedLoanBreakdown> {
    return this.reportRepository.getInterestEarnedLoanBreakdown(loanId, fromDate, toDate);
  }
}
