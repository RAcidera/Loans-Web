import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';
import { CashLedgerEntry, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';

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

  getLedgerEntries(): Observable<CashLedgerEntry[]> {
    return this.http.get<CashLedgerEntry[]>(`${this.baseUrl}/cash-funds/ledger`);
  }

  /**
   * referenceId is accepted by this port for symmetry with the domain
   * model, but the backend's manual-entry endpoint deliberately doesn't
   * take one — loan_release and payment_received (the only entry types
   * that would have a referenceId) are created automatically via domain
   * events, never through this endpoint. See
   * AddCashTransactionCommandHandler on the backend for why.
   */
  addTransaction(
    transactionType: CashTransactionType,
    amount: number,
    remarks: string,
    _referenceId?: string,
  ): Observable<CashLedgerEntry> {
    return this.http.post<CashLedgerEntry>(`${this.baseUrl}/cash-funds/ledger`, { transactionType, amount, remarks });
  }
}
