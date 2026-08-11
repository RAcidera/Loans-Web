import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DeletePaymentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, paymentId: string): Observable<Loan> {
    return this.loanRepository.deletePayment(loanId, paymentId);
  }
}
