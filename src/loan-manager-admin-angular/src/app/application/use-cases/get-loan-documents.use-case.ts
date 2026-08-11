import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanDocument } from '../../domain/entities/loan-document.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetLoanDocumentsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<LoanDocument[]> {
    return this.loanRepository.getLoanDocuments(loanId);
  }
}
