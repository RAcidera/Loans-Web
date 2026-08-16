import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

/** The Cash Transactions grid's row-menu "Delete" action — manually-entered rows only. */
@Injectable({ providedIn: 'root' })
export class DeleteCashTransactionUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(ledgerId: string): Observable<void> {
    return this.cashLedgerRepository.deleteTransaction(ledgerId);
  }
}
