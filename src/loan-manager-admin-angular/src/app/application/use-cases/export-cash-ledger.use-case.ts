import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CashLedgerPageFilters } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

/** Cash Transactions grid "Export" button — downloads the whole filtered result set as an .xlsx. */
@Injectable({ providedIn: 'root' })
export class ExportCashLedgerUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(filters?: CashLedgerPageFilters): Observable<Blob> {
    return this.cashLedgerRepository.exportLedger(filters);
  }
}
