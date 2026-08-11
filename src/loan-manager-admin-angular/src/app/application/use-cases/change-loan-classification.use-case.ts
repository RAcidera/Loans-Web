import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan, LoanClassification } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class ChangeLoanClassificationUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, classification: LoanClassification): Observable<Loan> {
    return this.loanRepository.changeLoanClassification(loanId, classification);
  }
}
