import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { PaymentPageFilters, PaymentWithCustomer } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetPaymentsPageUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc', filters?: PaymentPageFilters,
  ): Observable<PagedResult<PaymentWithCustomer>> {
    return this.loanRepository.getPaymentsListPage(pageIndex, pageSize, sortBy, sortDir, filters);
  }
}
