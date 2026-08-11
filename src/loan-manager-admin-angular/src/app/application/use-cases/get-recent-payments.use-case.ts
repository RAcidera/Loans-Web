import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PaymentWithCustomer } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetRecentPaymentsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(limit = 6): Observable<PaymentWithCustomer[]> {
    return this.loanRepository.getRecentPayments(limit);
  }
}
