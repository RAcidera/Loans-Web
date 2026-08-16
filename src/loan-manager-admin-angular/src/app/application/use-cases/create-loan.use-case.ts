import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class CreateLoanUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    customerId: string, principal: number, interestRate?: number, paymentTermsMonths?: number,
    startDate?: string, interestAmount?: number,
  ): Observable<Loan> {
    return this.loanRepository.createLoan(customerId, principal, interestRate, paymentTermsMonths, startDate, interestAmount);
  }
}
