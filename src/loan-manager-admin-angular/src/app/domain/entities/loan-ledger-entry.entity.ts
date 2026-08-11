// Domain layer — the SRS's "Additional Recommendation: Loan Ledger":
// a per-loan, append-only financial history backing the Payments/Extensions
// tabs' Running Balance columns.

export type LoanLedgerTransactionType = 'loan_released' | 'interest_added' | 'payment' | 'extension';

export interface LoanLedgerEntry {
  ledgerId: string;
  loanId: string;
  transactionDate: string;
  transactionType: LoanLedgerTransactionType;
  /** PaymentId or LoanExtensionId, when this row corresponds to one — lets a Payments/Extensions row look up its own Running Balance directly. */
  referenceId?: string;
  debit: number;
  credit: number;
  runningBalance: number;
  remarks: string;
  createdAt: string;
}
