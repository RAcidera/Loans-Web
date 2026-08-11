import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

/** Loan Details "Audit Log" tab. */
@Injectable({ providedIn: 'root' })
export class GetLoanAuditLogUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<LoanAuditLogEntry[]> {
    return this.loanRepository.getLoanAuditLog(loanId);
  }
}
