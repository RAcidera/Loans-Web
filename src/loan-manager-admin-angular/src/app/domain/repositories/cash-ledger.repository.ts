import { Observable } from 'rxjs';
import { CashLedgerEntry, CashLedgerPageFilters, CashLedgerTotals, CashSummary, CashTransactionType } from '../entities/cash-ledger-entry.entity';
import { PagedResult } from '../entities/paged-result.entity';

/**
 * Port covering the Cash_Ledger table and the funds formulas in SRS
 * section "0. Cash Ledger / Funds Tracking". Kept separate from
 * LoanRepository because the SRS treats funds tracking as its own concern —
 * cash on hand is computed from actual cash movement, not from loan
 * receivables.
 */
export abstract class CashLedgerRepository {
  abstract getSummary(): Observable<CashSummary>;
  abstract getLedgerPage(pageIndex: number, pageSize: number, filters?: CashLedgerPageFilters): Observable<PagedResult<CashLedgerEntry>>;
  abstract getLedgerTotals(filters?: CashLedgerPageFilters): Observable<CashLedgerTotals>;
  abstract exportLedger(filters?: CashLedgerPageFilters): Observable<Blob>;

  abstract addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate?: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry>;

  abstract editTransaction(
    ledgerId: string,
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry>;

  abstract deleteTransaction(ledgerId: string): Observable<void>;
}
