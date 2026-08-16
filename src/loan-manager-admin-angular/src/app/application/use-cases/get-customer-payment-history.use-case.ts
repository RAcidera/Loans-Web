import { Injectable } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { CustomerPayment } from '../../domain/entities/customer-payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/**
 * Flattens every payment across all of a customer's loans into one
 * newest-first list — used for the Customer Profile's "Last Payment" KPI
 * and "Recent Activity" feed, which need the whole history to find the
 * true most-recent payment, not just one page of it. The Payment History
 * tab's own table uses the paged GetCustomerPaymentsPageUseCase instead —
 * see that use-case for the server-side-paged version of this same data.
 */
@Injectable({ providedIn: 'root' })
export class GetCustomerPaymentHistoryUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(customerId: string): Observable<CustomerPayment[]> {
    return this.loanRepository.getLoansByCustomer(customerId).pipe(
      switchMap((loans) =>
        loans.length === 0
          ? of([])
          : forkJoin(
              loans.map((loan) =>
                this.loanRepository
                  .getPayments(loan.loanId)
                  .pipe(map((payments) => payments.map((p): CustomerPayment => ({ ...p, loanNumber: loan.loanNumber })))),
              ),
            ),
      ),
      map((groups) => groups.flat().sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime())),
    );
  }
}
