import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerEntry } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

@Injectable({ providedIn: 'root' })
export class GetCashLedgerUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(): Observable<CashLedgerEntry[]> {
    return this.cashLedgerRepository.getLedgerEntries();
  }
}
