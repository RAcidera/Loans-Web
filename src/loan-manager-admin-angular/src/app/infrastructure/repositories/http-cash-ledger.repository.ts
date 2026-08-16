import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';
import { CashLedgerEntry, CashLedgerPageFilters, CashLedgerTotals, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';

function toFilterParams(filters?: CashLedgerPageFilters): Record<string, string> {
  if (!filters) return {};
  const params: Record<string, string> = {};
  if (filters.search) params['search'] = filters.search;
  if (filters.transactionType) params['type'] = filters.transactionType;
  if (filters.dateFrom) params['dateFrom'] = filters.dateFrom;
  if (filters.dateTo) params['dateTo'] = filters.dateTo;
  return params;
}

/**
 * Talks to CashFundsController on the .NET backend. Registered in
 * app.config.ts in place of MockCashLedgerRepository.
 */
@Injectable()
export class HttpCashLedgerRepository extends CashLedgerRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getSummary(): Observable<CashSummary> {
    return this.http.get<CashSummary>(`${this.baseUrl}/cash-funds/summary`);
  }

  getLedgerPage(pageIndex: number, pageSize: number, filters?: CashLedgerPageFilters): Observable<PagedResult<CashLedgerEntry>> {
    const params: Record<string, string | number> = { pageIndex, pageSize, ...toFilterParams(filters) };
    return this.http.get<PagedResult<CashLedgerEntry>>(`${this.baseUrl}/cash-funds/ledger/page`, { params });
  }

  getLedgerTotals(filters?: CashLedgerPageFilters): Observable<CashLedgerTotals> {
    return this.http.get<CashLedgerTotals>(`${this.baseUrl}/cash-funds/ledger/page/totals`, { params: toFilterParams(filters) });
  }

  exportLedger(filters?: CashLedgerPageFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/cash-funds/ledger/export`, { params: toFilterParams(filters), responseType: 'blob' });
  }

  addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate?: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry> {
    return this.http.post<CashLedgerEntry>(`${this.baseUrl}/cash-funds/ledger`, { transactionType, amount, remarks, transactionDate, isCashIn });
  }

  editTransaction(
    ledgerId: string,
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    transactionDate: string,
    isCashIn?: boolean,
  ): Observable<CashLedgerEntry> {
    return this.http.put<CashLedgerEntry>(`${this.baseUrl}/cash-funds/ledger/${ledgerId}`, { transactionType, amount, remarks, transactionDate, isCashIn });
  }

  deleteTransaction(ledgerId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/cash-funds/ledger/${ledgerId}`);
  }
}
