import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Customer } from '../../domain/entities/customer.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetCustomersUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(): Observable<Customer[]> {
    return this.loanRepository.getCustomers();
  }
}
