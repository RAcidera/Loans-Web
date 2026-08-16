import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestEarnedFilters, InterestEarnedOverview } from '../../domain/entities/interest-earned-report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetInterestEarnedOverviewUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(filters: InterestEarnedFilters): Observable<InterestEarnedOverview> {
    return this.reportRepository.getInterestEarnedOverview(filters);
  }
}
