import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerStatus } from '../../domain/entities/customer.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { CustomerListItem, LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetCustomersPageUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
    status?: CustomerStatus,
  ): Observable<PagedResult<CustomerListItem>> {
    return this.loanRepository.getCustomersPage(pageIndex, pageSize, search, sortBy, sortDir, status);
  }
}
