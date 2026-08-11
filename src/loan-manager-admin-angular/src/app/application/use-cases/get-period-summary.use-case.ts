import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PeriodSummary } from '../../domain/entities/report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetPeriodSummaryUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(startDate: string, endDate: string): Observable<PeriodSummary> {
    return this.reportRepository.getPeriodSummary(startDate, endDate);
  }
}
