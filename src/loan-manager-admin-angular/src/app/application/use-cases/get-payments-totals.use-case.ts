import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PaymentPageFilters, PaymentsTotals } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Payments list "footer total" row — same filters as GetPaymentsPageUseCase, no paging. */
@Injectable({ providedIn: 'root' })
export class GetPaymentsTotalsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(filters?: PaymentPageFilters): Observable<PaymentsTotals> {
    return this.loanRepository.getPaymentsListTotals(filters);
  }
}
