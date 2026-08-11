import { Injectable } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { Customer } from '../../domain/entities/customer.entity';
import { DashboardReceivables } from '../../domain/entities/dashboard-receivables.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

export interface CustomerDetail {
  customer: Customer | undefined;
  loans: Loan[];
  receivables: DashboardReceivables;
}

/**
 * Composes three port calls into the single view the Customer Profile page
 * (SRS wireframe 4) needs: profile fields, loans scoped to this customer,
 * and this customer's Financial Summary. Mirrors GetLoanDetailUseCase's
 * composition of the Loan Details dialog.
 */
@Injectable({ providedIn: 'root' })
export class GetCustomerDetailUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(customerId: string): Observable<CustomerDetail> {
    return forkJoin({
      customer: this.loanRepository.getCustomerById(customerId),
      loans: this.loanRepository.getLoansByCustomer(customerId),
      receivables: this.loanRepository.getCustomerReceivables(customerId),
    });
  }
}
