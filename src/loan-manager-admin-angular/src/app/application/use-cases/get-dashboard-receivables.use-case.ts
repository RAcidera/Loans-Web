import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardReceivables } from '../../domain/entities/dashboard-receivables.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class GetDashboardReceivablesUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(): Observable<DashboardReceivables> {
    return this.loanRepository.getDashboardReceivables();
  }
}
