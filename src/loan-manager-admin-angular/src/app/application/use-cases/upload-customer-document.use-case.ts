import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerDocument } from '../../domain/entities/customer-document.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class UploadCustomerDocumentUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(customerId: string, file: File): Observable<CustomerDocument> {
    return this.loanRepository.uploadCustomerDocument(customerId, file);
  }
}
