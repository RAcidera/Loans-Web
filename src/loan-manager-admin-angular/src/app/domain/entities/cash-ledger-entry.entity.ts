// Domain layer — mirrors the Cash_Ledger table and the funds formulas in
// the SRS (section "0. Cash Ledger / Funds Tracking"), redesigned per the
// Cash Ledger UX review: Cash In/Cash Out split into separate columns
// (rather than a signed amount), a running balance per row, and Adjustment
// as a manual type whose direction (isCashIn) isn't fixed the way every
// other type's is.

export type CashTransactionType =
  | 'loan_release'
  | 'payment_received'
  | 'owner_deposit'
  | 'owner_withdrawal'
  | 'expense'
  | 'adjustment';

export interface CashLedgerEntry {
  ledgerId: string;
  transactionDate: string;
  transactionType: CashTransactionType;
  referenceId: string | null; // loan number, nullable
  amount: number;
  /** Whether amount is Cash In or Cash Out — authoritative from the server, since Adjustment's direction isn't derivable from transactionType alone. */
  isCashIn: boolean;
  /** True for loan_release/payment_received — system-generated rows the grid's row menu never offers Edit/Delete for. */
  isAutomatic: boolean;
  /** Cash on Hand immediately after this entry, computed over the full chronological ledger. Null only where the caller (Add/Edit response) didn't request one. */
  runningBalance: number | null;
  remarks: string;
  createdAt: string;
}

export interface CashLedgerTotals {
  cashIn: number;
  cashOut: number;
  netChange: number;
  count: number;
}

export interface CashLedgerPageFilters {
  search?: string;
  transactionType?: CashTransactionType;
  dateFrom?: string;
  dateTo?: string;
}

/**
 * The Cash Transactions page's summary card: current Cash on Hand plus This
 * Month's Cash In/Cash Out/Net Change as secondary context (why the total
 * moved), each compared against last calendar month. Total revolving funds
 * and outstanding principal were deliberately dropped — those are
 * loan/dashboard concerns, not "what cash do I have" ones.
 */
export interface CashSummary {
  cashOnHand: number;
  asOfDate: string;
  cashInThisMonth: number;
  cashOutThisMonth: number;
  netChangeThisMonth: number;
  cashOnHandChangePercent: number | null;
  cashInChangePercent: number | null;
  cashOutChangePercent: number | null;
  netChangePercent: number | null;
}
