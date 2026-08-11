import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanDocument } from '../../domain/entities/loan-document.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class UploadLoanDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, file: File): Observable<LoanDocument> {
    return this.loanRepository.uploadLoanDocument(loanId, file);
  }
}
