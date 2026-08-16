import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestEarnedFilters, InterestEarnedMonthlyPoint } from '../../domain/entities/interest-earned-report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetInterestEarnedMonthlyChartUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(filters: InterestEarnedFilters): Observable<InterestEarnedMonthlyPoint[]> {
    return this.reportRepository.getInterestEarnedMonthlyChart(filters);
  }
}
