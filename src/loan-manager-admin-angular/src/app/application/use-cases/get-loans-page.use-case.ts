import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan, LoanPageFilters } from '../../domain/entities/loan.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetLoansPageUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
    filters?: LoanPageFilters,
  ): Observable<PagedResult<Loan>> {
    return this.loanRepository.getLoansPage(pageIndex, pageSize, search, sortBy, sortDir, filters);
  }
}
