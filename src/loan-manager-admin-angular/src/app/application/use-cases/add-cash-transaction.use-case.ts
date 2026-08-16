import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerEntry, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

@Injectable({ providedIn: 'root' })
export class AddCashTransactionUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(transactionType: CashTransactionType, amount: number, remarks: string, transactionDate?: string, isCashIn?: boolean): Observable<CashLedgerEntry> {
    return this.cashLedgerRepository.addTransaction(transactionType, amount, remarks, transactionDate, isCashIn);
  }
}
