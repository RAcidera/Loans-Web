import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DownloadLoanDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, documentId: string): Observable<Blob> {
    return this.loanRepository.downloadLoanDocument(loanId, documentId);
  }
}
