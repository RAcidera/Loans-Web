import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerStatus } from '../../domain/entities/customer.entity';
import { CustomerTotals } from '../../domain/entities/customer-totals.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetCustomersTotalsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(search: string, status?: CustomerStatus): Observable<CustomerTotals> {
    return this.loanRepository.getCustomersTotals(search, status);
  }
}
