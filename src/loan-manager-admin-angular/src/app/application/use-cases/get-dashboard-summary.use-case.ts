import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardSummary } from '../../domain/entities/dashboard-summary.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetDashboardSummaryUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(): Observable<DashboardSummary> {
    return this.loanRepository.getDashboardSummary();
  }
}
