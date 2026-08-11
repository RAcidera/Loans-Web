import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DeleteLoanDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, documentId: string): Observable<void> {
    return this.loanRepository.deleteLoanDocument(loanId, documentId);
  }
}
