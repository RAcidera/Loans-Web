import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ReportRepository } from '../../domain/repositories/report.repository';

@Injectable({ providedIn: 'root' })
export class ExportPeriodReportUseCase {
  constructor(private readonly reportRepository: ReportRepository) {}

  execute(startDate: string, endDate: string): Observable<Blob> {
    return this.reportRepository.exportPeriodReportCsv(startDate, endDate);
  }
}
