// Domain layer — mirrors the Cash_Ledger table and the funds formulas in
// the SRS (section "0. Cash Ledger / Funds Tracking").

export type CashTransactionType =
  | 'loan_release'
  | 'payment_received'
  | 'owner_deposit'
  | 'owner_withdrawal'
  | 'expense';

export interface CashLedgerEntry {
  ledgerId: string;
  transactionDate: string;
  transactionType: CashTransactionType;
  referenceId: string | null; // loan_id, nullable
  amount: number;
  remarks: string;
  createdAt: string;
}

/**
 * Precomputed view of Formulas 1-5 from the SRS:
 *   cashOnHand = totalCashIn - totalCashOut
 *   revolvingFunds = cashOnHand (cash-based, not receivables-based)
 * sevenDayTrend is a presentation aid (normalized 0-1) showing the last 7
 * days of cash-on-hand movement, not part of the SRS formulas themselves.
 */
export interface CashSummary {
  totalCashIn: number;
  totalCashOut: number;
  cashOnHand: number;
  revolvingFunds: number;
  outstandingPrincipal: number;
  sevenDayTrend: number[];
}
