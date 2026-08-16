import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CustomerPayment } from '../../domain/entities/customer-payment.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Server-side paging for the Customer Profile's "Payment History" tab table. */
@Injectable({ providedIn: 'root' })
export class GetCustomerPaymentsPageUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    customerId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<CustomerPayment>> {
    return this.loanRepository.getCustomerPaymentsPage(customerId, pageIndex, pageSize, sortBy, sortDir);
  }
}
