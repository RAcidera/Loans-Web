import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DeleteCustomerDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(customerId: string, documentId: string): Observable<void> {
    return this.loanRepository.deleteCustomerDocument(customerId, documentId);
  }
}
