import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashSummary } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

@Injectable({ providedIn: 'root' })
export class GetCashSummaryUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(): Observable<CashSummary> {
    return this.cashLedgerRepository.getSummary();
  }
}
