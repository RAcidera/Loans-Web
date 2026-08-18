import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { CashLedgerEntry } from '../../domain/entities/cash-ledger-entry.entity';
import { CashLedgerRepository } from '../../domain/repositories/cash-ledger.repository';

/** Unreferenced by any component (the Cash & Funds page uses getLedgerPage's server-side paging directly) — kept for API symmetry with the other entity-list use cases. getLedgerEntries() was never a real CashLedgerRepository method; fixed to call the actual getLedgerPage port method instead of leaving this uncompilable. */
@Injectable({ providedIn: 'root' })
export class GetCashLedgerUseCase {
  constructor(private readonly cashLedgerRepository: CashLedgerRepository) {}

  execute(): Observable<CashLedgerEntry[]> {
    return this.cashLedgerRepository.getLedgerPage(0, Number.MAX_SAFE_INTEGER).pipe(map((page) => page.items));
  }
}
