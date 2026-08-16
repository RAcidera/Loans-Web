import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanPageFilters } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Loans list "Export" button — downloads the whole filtered result set as an .xlsx. */
@Injectable({ providedIn: 'root' })
export class ExportLoansUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(search: string, filters?: LoanPageFilters): Observable<Blob> {
    return this.loanRepository.exportLoans(search, filters);
  }
}
