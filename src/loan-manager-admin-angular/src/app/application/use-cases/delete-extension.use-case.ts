import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Loan } from '../../domain/entities/loan.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class DeleteExtensionUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(loanId: string, extensionId: string): Observable<Loan> {
    return this.loanRepository.deleteExtension(loanId, extensionId);
  }
}
