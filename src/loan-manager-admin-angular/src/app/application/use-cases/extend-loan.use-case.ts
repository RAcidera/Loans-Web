import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class ExtendLoanUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, extensionDays: number, remarks: string, additionalChargesAmount = 0): Observable<Loan> {
    return this.loanRepository.extendLoan(loanId, extensionDays, remarks, additionalChargesAmount);
  }
}
