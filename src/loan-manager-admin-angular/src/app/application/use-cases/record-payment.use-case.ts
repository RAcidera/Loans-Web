import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Payment, PaymentMethod } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class RecordPaymentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, amountPaid: number, paymentMethod: PaymentMethod, notes: string, referenceNumber?: string): Observable<Payment> {
    return this.loanRepository.recordPayment(loanId, amountPaid, paymentMethod, notes, referenceNumber);
  }
}
