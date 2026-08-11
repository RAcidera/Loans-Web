import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetLoansUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(): Observable<Loan[]> {
    return this.loanRepository.getLoans();
  }
}
