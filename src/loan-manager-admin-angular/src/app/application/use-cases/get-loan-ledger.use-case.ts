import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** The SRS's "Additional Recommendation: Loan Ledger" — backs the Payments/Extensions tabs' Running Balance columns. */
@Injectable({ providedIn: 'root' })
export class GetLoanLedgerUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<LoanLedgerEntry[]> {
    return this.loanRepository.getLoanLedger(loanId);
  }
}
