import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InterestEarnedFilters } from '../../domain/entities/interest-earned-report.entity';
import { ReportRepository } from '../../domain/repositories/report.repository';

/** Interest Earned Report's "Export Excel" / "Export PDF" buttons — downloads the whole filtered result set. */
@Injectable({ providedIn: 'root' })
export class ExportInterestEarnedXlsxUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(filters: InterestEarnedFilters): Observable<Blob> {
    return this.reportRepository.exportInterestEarnedXlsx(filters);
  }
}

@Injectable({ providedIn: 'root' })
export class ExportInterestEarnedPdfUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(filters: InterestEarnedFilters): Observable<Blob> {
    return this.reportRepository.exportInterestEarnedPdf(filters);
  }
}
