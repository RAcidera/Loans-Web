import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';

import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';
import { CashLedgerEntry, CashLedgerPageFilters, CashLedgerTotals, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';

const CASH_IN_TYPES: CashTransactionType[] = ['payment_received', 'owner_deposit'];
const AUTOMATIC_TYPES: CashTransactionType[] = ['loan_release', 'payment_received'];

interface RawEntry {
  ledgerId: string;
  transactionDate: string;
  transactionType: CashTransactionType;
  referenceId: string | null;
  amount: number;
  isCashIn: boolean;
  remarks: string;
  createdAt: string;
}

const INITIAL_ENTRIES: RawEntry[] = [
  { ledgerId: 'CL-001', transactionDate: '2026-06-01', transactionType: 'owner_deposit', referenceId: null, amount: 20000, isCashIn: true, remarks: 'Initial capital', createdAt: '2026-06-01' },
  { ledgerId: 'CL-002', transactionDate: '2026-06-10', transactionType: 'loan_release', referenceId: 'L-2034', amount: 5000, isCashIn: false, remarks: 'Loan release — Maria Santos', createdAt: '2026-06-10' },
  { ledgerId: 'CL-003', transactionDate: '2026-06-20', transactionType: 'loan_release', referenceId: 'L-2036', amount: 2000, isCashIn: false, remarks: 'Loan release — Liza Ramos', createdAt: '2026-06-20' },
  { ledgerId: 'CL-004', transactionDate: '2026-06-25', transactionType: 'loan_release', referenceId: 'L-2038', amount: 1000, isCashIn: false, remarks: 'Loan release — Ana Villanueva', createdAt: '2026-06-25' },
  { ledgerId: 'CL-005', transactionDate: '2026-07-01', transactionType: 'payment_received', referenceId: 'L-2035', amount: 1545, isCashIn: true, remarks: 'Payment — Jun Dela Cruz', createdAt: '2026-07-01' },
  { ledgerId: 'CL-006', transactionDate: '2026-07-05', transactionType: 'expense', referenceId: null, amount: 300, isCashIn: false, remarks: 'Ledger book, transport', createdAt: '2026-07-05' },
  { ledgerId: 'CL-007', transactionDate: '2026-07-10', transactionType: 'payment_received', referenceId: 'L-2034', amount: 1000, isCashIn: true, remarks: 'Payment — Maria Santos', createdAt: '2026-07-10' },
  { ledgerId: 'CL-008', transactionDate: '2026-07-14', transactionType: 'payment_received', referenceId: 'L-2035', amount: 1545, isCashIn: true, remarks: 'Payment — Jun Dela Cruz (settlement)', createdAt: '2026-07-14' },
  { ledgerId: 'CL-009', transactionDate: '2026-07-20', transactionType: 'payment_received', referenceId: 'L-2038', amount: 400, isCashIn: true, remarks: 'Payment — Ana Villanueva', createdAt: '2026-07-20' },
  { ledgerId: 'CL-010', transactionDate: '2026-07-25', transactionType: 'payment_received', referenceId: 'L-2034', amount: 1000, isCashIn: true, remarks: 'Payment — Maria Santos', createdAt: '2026-07-25' },
  { ledgerId: 'CL-011', transactionDate: '2026-07-30', transactionType: 'owner_withdrawal', referenceId: null, amount: 2000, isCashIn: false, remarks: 'Owner draw', createdAt: '2026-07-30' },
];

/** Stand-in data source implementing CashLedgerRepository, mirroring the .NET backend's filtering/running-balance/manual-edit rules closely enough for offline demo use. */
@Injectable()
export class MockCashLedgerRepository extends CashLedgerRepository {
  private entries: RawEntry[] = [...INITIAL_ENTRIES];

  private toDto(e: RawEntry, runningBalance: number | null): CashLedgerEntry {
    return { ...e, isAutomatic: AUTOMATIC_TYPES.includes(e.transactionType), runningBalance };
  }

  private chronological(): RawEntry[] {
    return [...this.entries].sort((a, b) => a.transactionDate.localeCompare(b.transactionDate) || a.createdAt.localeCompare(b.createdAt));
  }

  private runningBalances(): Map<string, number> {
    const balances = new Map<string, number>();
    let balance = 0;
    for (const e of this.chronological()) {
      balance += e.isCashIn ? e.amount : -e.amount;
      balances.set(e.ledgerId, balance);
    }
    return balances;
  }

  private applyFilters(filters?: CashLedgerPageFilters): RawEntry[] {
    let result = this.entries;
    if (filters?.search) {
      const term = filters.search.toLowerCase();
      result = result.filter((e) => (e.referenceId ?? '').toLowerCase().includes(term) || e.remarks.toLowerCase().includes(term));
    }
    if (filters?.transactionType) result = result.filter((e) => e.transactionType === filters.transactionType);
    if (filters?.dateFrom) result = result.filter((e) => e.transactionDate >= filters.dateFrom!);
    if (filters?.dateTo) result = result.filter((e) => e.transactionDate <= filters.dateTo!);
    return result;
  }

  private computeSummary(): CashSummary {
    const cashOnHand = this.entries.reduce((sum, e) => sum + (e.isCashIn ? e.amount : -e.amount), 0);
    const today = new Date().toISOString().slice(0, 10);
    const monthStart = today.slice(0, 8) + '01';

    const thisMonth = this.entries.filter((e) => e.transactionDate >= monthStart && e.transactionDate <= today);
    const cashInThisMonth = thisMonth.filter((e) => e.isCashIn).reduce((s, e) => s + e.amount, 0);
    const cashOutThisMonth = thisMonth.filter((e) => !e.isCashIn).reduce((s, e) => s + e.amount, 0);

    return {
      cashOnHand,
      asOfDate: today,
      cashInThisMonth,
      cashOutThisMonth,
      netChangeThisMonth: cashInThisMonth - cashOutThisMonth,
      cashOnHandChangePercent: null,
      cashInChangePercent: null,
      cashOutChangePercent: null,
      netChangePercent: null,
      grossReceivables: 0,
    };
  }

  getSummary(): Observable<CashSummary> {
    return of(this.computeSummary()).pipe(delay(150));
  }

  getLedgerPage(pageIndex: number, pageSize: number, filters?: CashLedgerPageFilters): Observable<PagedResult<CashLedgerEntry>> {
    const balances = this.runningBalances();
    const filtered = this.applyFilters(filters).sort((a, b) => b.transactionDate.localeCompare(a.transactionDate) || b.createdAt.localeCompare(a.createdAt));
    const items = filtered.slice(pageIndex * pageSize, pageIndex * pageSize + pageSize).map((e) => this.toDto(e, balances.get(e.ledgerId) ?? null));
    return of({ items, totalCount: filtered.length }).pipe(delay(150));
  }

  getLedgerTotals(filters?: CashLedgerPageFilters): Observable<CashLedgerTotals> {
    const filtered = this.applyFilters(filters);
    const cashIn = filtered.filter((e) => e.isCashIn).reduce((s, e) => s + e.amount, 0);
    const cashOut = filtered.filter((e) => !e.isCashIn).reduce((s, e) => s + e.amount, 0);
    return of({ cashIn, cashOut, netChange: cashIn - cashOut, count: filtered.length }).pipe(delay(150));
  }

  exportLedger(): Observable<Blob> {
    return of(new Blob([''], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })).pipe(delay(150));
  }

  addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate?: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry> {
    const entry: RawEntry = {
      ledgerId: `CL-${Math.floor(Math.random() * 9000 + 1000)}`,
      transactionDate: transactionDate ?? new Date().toISOString().slice(0, 10),
      transactionType,
      referenceId: null,
      amount,
      isCashIn: transactionType === 'adjustment' ? !!isCashIn : CASH_IN_TYPES.includes(transactionType),
      remarks,
      createdAt: new Date().toISOString(),
    };
    this.entries.push(entry);
    return of(this.toDto(entry, null)).pipe(delay(150));
  }

  editTransaction(
    ledgerId: string,
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry> {
    const entry = this.entries.find((e) => e.ledgerId === ledgerId);
    if (!entry) throw new Error(`Cash transaction '${ledgerId}' was not found.`);
    entry.transactionType = transactionType;
    entry.amount = amount;
    entry.remarks = remarks;
    entry.transactionDate = transactionDate;
    entry.isCashIn = transactionType === 'adjustment' ? !!isCashIn : CASH_IN_TYPES.includes(transactionType);
    return of(this.toDto(entry, null)).pipe(delay(150));
  }

  deleteTransaction(ledgerId: string): Observable<void> {
    this.entries = this.entries.filter((e) => e.ledgerId !== ledgerId);
    return of(undefined).pipe(delay(150));
  }
}
