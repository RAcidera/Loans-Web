import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Payment, PaymentMethod } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class UpdatePaymentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    loanId: string, paymentId: string, amountPaid: number, paymentMethod: PaymentMethod,
    notes?: string, referenceNumber?: string,
  ): Observable<Payment> {
    return this.loanRepository.updatePayment(loanId, paymentId, amountPaid, paymentMethod, notes, referenceNumber);
  }
}
