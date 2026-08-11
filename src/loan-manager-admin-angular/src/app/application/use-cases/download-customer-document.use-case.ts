import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DownloadCustomerDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(customerId: string, documentId: string): Observable<Blob> {
    return this.loanRepository.downloadCustomerDocument(customerId, documentId);
  }
}
