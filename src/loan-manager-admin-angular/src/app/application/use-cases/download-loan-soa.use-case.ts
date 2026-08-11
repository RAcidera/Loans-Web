import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Spec's Statement of Account PDF — the Loan Details "Generate SOA" button. */
@Injectable({ providedIn: 'root' })
export class DownloadLoanSoaUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<Blob> {
    return this.loanRepository.downloadLoanSoa(loanId);
  }
}
