import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PagedResult } from '../../domain/entities/paged-result.entity';
import { Payment } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Server-side paging for the Loan Details "Payments" tab table. */
@Injectable({ providedIn: 'root' })
export class GetLoanPaymentsPageUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    loanId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<Payment>> {
    return this.loanRepository.getPaymentsPage(loanId, pageIndex, pageSize, sortBy, sortDir);
  }
}
