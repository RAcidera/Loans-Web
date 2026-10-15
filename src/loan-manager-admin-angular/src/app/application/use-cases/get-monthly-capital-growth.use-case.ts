import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CapitalGrowthRange, MonthlyCapitalGrowth } from '../../domain/entities/capital-growth.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetMonthlyCapitalGrowthUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(months: CapitalGrowthRange): Observable<MonthlyCapitalGrowth> {
    return this.loanRepository.getMonthlyCapitalGrowth(months);
  }
}
