import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PaymentPageFilters } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Payments list "Export" button — downloads the whole filtered result set as an .xlsx. */
@Injectable({ providedIn: 'root' })
export class ExportPaymentsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(filters?: PaymentPageFilters): Observable<Blob> {
    return this.loanRepository.exportPaymentsList(filters);
  }
}
