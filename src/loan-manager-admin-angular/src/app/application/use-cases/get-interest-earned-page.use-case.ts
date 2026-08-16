import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestEarnedFilters, InterestEarnedRow } from '../../domain/entities/interest-earned-report.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class GetInterestEarnedPageUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(pageIndex: number, pageSize: number, filters: InterestEarnedFilters, sortBy?: string, sortDir?: 'asc' | 'desc'): Observable<PagedResult<InterestEarnedRow>> {
    return this.reportRepository.getInterestEarnedPage(pageIndex, pageSize, filters, sortBy, sortDir);
  }
}
