import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerStatus } from '../../domain/entities/customer.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Customers list "Export" button — downloads the whole filtered result set as an .xlsx. */
@Injectable({ providedIn: 'root' })
export class ExportCustomersUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(search: string, status?: CustomerStatus): Observable<Blob> {
    return this.loanRepository.exportCustomers(search, status);
  }
}
