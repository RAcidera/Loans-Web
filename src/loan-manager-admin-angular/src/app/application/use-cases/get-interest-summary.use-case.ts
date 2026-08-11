import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestSummary } from '../../domain/entities/report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetInterestSummaryUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(): Observable<InterestSummary> {
    return this.reportRepository.getInterestSummary();
  }
}
