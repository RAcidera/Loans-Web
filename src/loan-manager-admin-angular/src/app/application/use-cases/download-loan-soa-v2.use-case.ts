import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Statement of Account V2 — the Loan Details "Generate SOA V2" action, coexisting with DownloadLoanSoaUseCase. */
@Injectable({ providedIn: 'root' })
export class DownloadLoanSoaV2UseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<Blob> {
    return this.loanRepository.downloadLoanSoaV2(loanId);
  }
}
