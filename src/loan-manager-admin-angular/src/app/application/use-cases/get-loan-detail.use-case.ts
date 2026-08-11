import { Injectable } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';

import { Loan } from '../../domain/entities/loan.entity';
import { LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { Payment } from '../../domain/entities/payment.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

export interface LoanDetail {
  loan: Loan | undefined;
  extensions: LoanExtension[];
  payments: Payment[];
  ledger: LoanLedgerEntry[];
  auditLog: LoanAuditLogEntry[];
}

/**
 * Composes five port calls into the single view the Loan Details page (SRS
 * wireframe 5) needs: loan summary, extension history, payment history,
 * ledger (Running Balance columns), audit log (Audit Log tab).
 */
@Injectable({ providedIn: 'root' })
export class GetLoanDetailUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string): Observable<LoanDetail> {
    return forkJoin({
      loan: this.loanRepository.getLoanById(loanId),
      extensions: this.loanRepository.getExtensions(loanId),
      payments: this.loanRepository.getPayments(loanId),
      ledger: this.loanRepository.getLoanLedger(loanId),
      auditLog: this.loanRepository.getLoanAuditLog(loanId),
    }).pipe(map((result) => result));
  }
}
