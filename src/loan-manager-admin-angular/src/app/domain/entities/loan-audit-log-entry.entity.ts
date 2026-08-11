// Domain layer — backs the Loan Details "Audit Log" tab: who changed what,
// and when, for the loan's own fields/classification/write-off (financial
// movements are LoanLedgerEntry instead).

export type LoanAuditAction = 'edited' | 'classification_changed' | 'written_off';

export interface LoanAuditLogEntry {
  auditLogId: string;
  loanId: string;
  action: LoanAuditAction;
  description: string;
  performedBy: string;
  occurredAt: string;
}
