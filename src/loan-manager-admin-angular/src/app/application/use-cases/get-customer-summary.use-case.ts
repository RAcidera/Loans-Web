import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CustomerSummary } from '../../domain/entities/report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetCustomerSummaryUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(): Observable<CustomerSummary[]> {
    return this.reportRepository.getCustomerSummary();
  }
}
