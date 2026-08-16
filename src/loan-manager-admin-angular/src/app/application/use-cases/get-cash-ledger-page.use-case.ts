import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerEntry, CashLedgerPageFilters } from '../../domain/entities/cash-ledger-entry.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

@Injectable({ providedIn: 'root' })
export class GetCashLedgerPageUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(pageIndex: number, pageSize: number, filters?: CashLedgerPageFilters): Observable<PagedResult<CashLedgerEntry>> {
    return this.cashLedgerRepository.getLedgerPage(pageIndex, pageSize, filters);
  }
}
