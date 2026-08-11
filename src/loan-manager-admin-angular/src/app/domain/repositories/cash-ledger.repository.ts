import { Observable } from 'rxjs';
import { CashLedgerEntry, CashSummary, CashTransactionType } from '../entities/cash-ledger-entry.entity';

/**
 * Port covering the Cash_Ledger table and the funds formulas in SRS
 * section "0. Cash Ledger / Funds Tracking". Kept separate from
 * LoanRepository because the SRS treats funds tracking as its own concern —
 * cash on hand and revolving funds are computed from actual cash movement,
 * not from loan receivables.
 */
export abstract class CashLedgerRepository {
  abstract getSummary(): Observable<CashSummary>;
  abstract getLedgerEntries(): Observable<CashLedgerEntry[]>;
  abstract addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    referenceId?: string,
  ): Observable<CashLedgerEntry>;
}
