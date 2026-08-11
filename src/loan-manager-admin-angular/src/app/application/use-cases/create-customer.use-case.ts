import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Customer } from '../../domain/entities/customer.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class CreateCustomerUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias?: string, notes?: string,
  ): Observable<Customer> {
    return this.loanRepository.createCustomer(fullName, address, contactNumber, borrowerType, nicknameAlias, notes);
  }
}
