import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';

import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';
import { CashLedgerEntry, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';

const CASH_IN_TYPES: CashTransactionType[] = ['payment_received', 'owner_deposit'];
const CASH_OUT_TYPES: CashTransactionType[] = ['loan_release', 'owner_withdrawal', 'expense'];

const INITIAL_ENTRIES: CashLedgerEntry[] = [
  { ledgerId: 'CL-001', transactionDate: '2026-06-01', transactionType: 'owner_deposit', referenceId: null, amount: 20000, remarks: 'Initial capital', createdAt: '2026-06-01' },
  { ledgerId: 'CL-002', transactionDate: '2026-06-10', transactionType: 'loan_release', referenceId: 'L-2034', amount: 5000, remarks: 'Loan release — Maria Santos', createdAt: '2026-06-10' },
  { ledgerId: 'CL-003', transactionDate: '2026-06-20', transactionType: 'loan_release', referenceId: 'L-2036', amount: 2000, remarks: 'Loan release — Liza Ramos', createdAt: '2026-06-20' },
  { ledgerId: 'CL-004', transactionDate: '2026-06-25', transactionType: 'loan_release', referenceId: 'L-2038', amount: 1000, remarks: 'Loan release — Ana Villanueva', createdAt: '2026-06-25' },
  { ledgerId: 'CL-005', transactionDate: '2026-07-01', transactionType: 'payment_received', referenceId: 'L-2035', amount: 1545, remarks: 'Payment — Jun Dela Cruz', createdAt: '2026-07-01' },
  { ledgerId: 'CL-006', transactionDate: '2026-07-05', transactionType: 'expense', referenceId: null, amount: 300, remarks: 'Ledger book, transport', createdAt: '2026-07-05' },
  { ledgerId: 'CL-007', transactionDate: '2026-07-10', transactionType: 'payment_received', referenceId: 'L-2034', amount: 1000, remarks: 'Payment — Maria Santos', createdAt: '2026-07-10' },
  { ledgerId: 'CL-008', transactionDate: '2026-07-14', transactionType: 'payment_received', referenceId: 'L-2035', amount: 1545, remarks: 'Payment — Jun Dela Cruz (settlement)', createdAt: '2026-07-14' },
  { ledgerId: 'CL-009', transactionDate: '2026-07-20', transactionType: 'payment_received', referenceId: 'L-2038', amount: 400, remarks: 'Payment — Ana Villanueva', createdAt: '2026-07-20' },
  { ledgerId: 'CL-010', transactionDate: '2026-07-25', transactionType: 'payment_received', referenceId: 'L-2034', amount: 1000, remarks: 'Payment — Maria Santos', createdAt: '2026-07-25' },
  { ledgerId: 'CL-011', transactionDate: '2026-07-30', transactionType: 'owner_withdrawal', referenceId: null, amount: 2000, remarks: 'Owner draw', createdAt: '2026-07-30' },
];

/**
 * Stand-in data source implementing CashLedgerRepository. Computes Formulas
 * 1-5 from the SRS directly off the ledger, exactly as specified: revolving
 * funds are cash-based, not receivables-based.
 */
@Injectable()
export class MockCashLedgerRepository extends CashLedgerRepository {
  private entries: CashLedgerEntry[] = [...INITIAL_ENTRIES];

  private computeSummary(): CashSummary {
    const totalCashIn = this.entries
      .filter((e) => CASH_IN_TYPES.includes(e.transactionType))
      .reduce((sum, e) => sum + e.amount, 0);

    const totalCashOut = this.entries
      .filter((e) => CASH_OUT_TYPES.includes(e.transactionType))
      .reduce((sum, e) => sum + e.amount, 0);

    const cashOnHand = totalCashIn - totalCashOut;

    // Outstanding principal would normally come from the Loans table (see
    // Formula 4). Kept as a static illustrative figure here since this
    // repository has no reference to LoanRepository — see the note in
    // recordPayment() in mock-loan.repository.ts about cross-boundary calls.
    const outstandingPrincipal = 9500;

    return {
      totalCashIn,
      totalCashOut,
      cashOnHand,
      revolvingFunds: cashOnHand, // Formula 5: cash-based, not receivables-based
      outstandingPrincipal,
      sevenDayTrend: [0.55, 0.6, 0.58, 0.65, 0.7, 0.68, 0.74],
    };
  }

  getSummary(): Observable<CashSummary> {
    return of(this.computeSummary()).pipe(delay(150));
  }

  getLedgerEntries(): Observable<CashLedgerEntry[]> {
    return of([...this.entries].reverse()).pipe(delay(150));
  }

  addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    referenceId: string | null = null,
  ): Observable<CashLedgerEntry> {
    const entry: CashLedgerEntry = {
      ledgerId: `CL-${Math.floor(Math.random() * 9000 + 1000)}`,
      transactionDate: new Date().toISOString().slice(0, 10),
      transactionType,
      referenceId,
      amount,
      remarks,
      createdAt: new Date().toISOString(),
    };
    this.entries.push(entry);
    return of(entry).pipe(delay(150));
  }
}
