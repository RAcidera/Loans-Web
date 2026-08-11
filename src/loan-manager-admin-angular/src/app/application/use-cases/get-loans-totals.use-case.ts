import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanPageFilters } from '../../domain/entities/loan.entity';
import { LoanTotals } from '../../domain/entities/loan-totals.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetLoansTotalsUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(search: string, filters?: LoanPageFilters): Observable<LoanTotals> {
    return this.loanRepository.getLoansTotals(search, filters);
  }
}
