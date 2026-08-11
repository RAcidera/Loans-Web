import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ReportRepository } from '../../domain/repositories/report.repository';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../../domain/entities/report.entity';

/**
 * Talks to ReportsController on the .NET backend. Registered in
 * app.config.ts in place of MockReportRepository.
 */
@Injectable()
export class HttpReportRepository extends ReportRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getInterestSummary(): Observable<InterestSummary> {
    return this.http.get<InterestSummary>(`${this.baseUrl}/reports/interest-summary`);
  }

  getCustomerSummary(): Observable<CustomerSummary[]> {
    return this.http.get<CustomerSummary[]>(`${this.baseUrl}/reports/customer-summary`);
  }

  getPeriodSummary(startDate: string, endDate: string): Observable<PeriodSummary> {
    return this.http.get<PeriodSummary>(`${this.baseUrl}/reports/period-summary`, {
      params: { start: startDate, end: endDate },
    });
  }

  exportPeriodReportCsv(startDate: string, endDate: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reports/export`, {
      params: { format: 'csv', start: startDate, end: endDate },
      responseType: 'blob',
    });
  }
}
