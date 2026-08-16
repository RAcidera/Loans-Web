import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanRepository } from '../../domain/repositories/loan.repository';

@Injectable({ providedIn: 'root' })
export class UpdateExtensionUseCase {
  constructor(private readonly loanRepository: LoanRepository) {}

  execute(
    loanId: string, extensionId: string, extensionDays: number,
    remarks: string, additionalChargesAmount = 0,
  ): Observable<LoanExtension> {
    return this.loanRepository.updateExtension(loanId, extensionId, extensionDays, remarks, additionalChargesAmount);
  }
}
