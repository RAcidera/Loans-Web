import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository, UpdateLoanFields } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class UpdateLoanUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, fields: UpdateLoanFields): Observable<Loan> {
    return this.loanRepository.updateLoan(loanId, fields);
  }
}
