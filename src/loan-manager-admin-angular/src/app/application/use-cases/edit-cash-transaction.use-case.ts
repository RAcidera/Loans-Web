import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerEntry, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

/** The Cash Transactions grid's row-menu "Edit" action — manually-entered rows only. */
@Injectable({ providedIn: 'root' })
export class EditCashTransactionUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(ledgerId: string, transactionType: CashTransactionType, amount: number, remarks: string, transactionDate: string, isCashIn?: boolean): Observable<CashLedgerEntry> {
    return this.cashLedgerRepository.editTransaction(ledgerId, transactionType, amount, remarks, transactionDate, isCashIn);
  }
}
