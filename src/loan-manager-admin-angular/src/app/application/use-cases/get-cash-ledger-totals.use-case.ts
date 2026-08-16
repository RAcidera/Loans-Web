import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerPageFilters, CashLedgerTotals } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

@Injectable({ providedIn: 'root' })
export class GetCashLedgerTotalsUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(filters?: CashLedgerPageFilters): Observable<CashLedgerTotals> {
    return this.cashLedgerRepository.getLedgerTotals(filters);
  }
}
