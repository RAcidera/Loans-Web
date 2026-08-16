import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ReportRepository } from '../../domain/repositories/report.repository';
import { InterestSummary, CustomerSummary, PeriodSummary } from '../../domain/entities/report.entity';
import {
  InterestEarnedFilters,
  InterestEarnedLoanBreakdown,
  InterestEarnedMonthlyPoint,
  InterestEarnedOverview,
  InterestEarnedRow,
} from '../../domain/entities/interest-earned-report.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';

function toFilterParams(filters: InterestEarnedFilters): Record<string, string> {
  const params: Record<string, string> = { fromDate: filters.fromDate, toDate: filters.toDate };
  if (filters.search) params['search'] = filters.search;
  if (filters.status) params['status'] = filters.status;
  if (filters.classification) params['classification'] = filters.classification;
  if (filters.interestType) params['interestType'] = filters.interestType;
  return params;
}

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

  getInterestEarnedPage(
    pageIndex: number, pageSize: number, filters: InterestEarnedFilters, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<InterestEarnedRow>> {
    const params: Record<string, string | number> = { pageIndex, pageSize, ...toFilterParams(filters) };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    return this.http.get<PagedResult<InterestEarnedRow>>(`${this.baseUrl}/reports/interest-earned/page`, { params });
  }

  getInterestEarnedOverview(filters: InterestEarnedFilters): Observable<InterestEarnedOverview> {
    return this.http.get<InterestEarnedOverview>(`${this.baseUrl}/reports/interest-earned/overview`, { params: toFilterParams(filters) });
  }

  getInterestEarnedMonthlyChart(filters: InterestEarnedFilters): Observable<InterestEarnedMonthlyPoint[]> {
    const { fromDate, toDate, ...rest } = toFilterParams(filters);
    return this.http.get<InterestEarnedMonthlyPoint[]>(`${this.baseUrl}/reports/interest-earned/monthly-chart`, { params: rest });
  }

  getInterestEarnedLoanBreakdown(loanId: string, fromDate: string, toDate: string): Observable<InterestEarnedLoanBreakdown> {
    return this.http.get<InterestEarnedLoanBreakdown>(`${this.baseUrl}/reports/interest-earned/${loanId}/breakdown`, {
      params: { fromDate, toDate },
    });
  }

  exportInterestEarnedXlsx(filters: InterestEarnedFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reports/interest-earned/export/xlsx`, { params: toFilterParams(filters), responseType: 'blob' });
  }

  exportInterestEarnedPdf(filters: InterestEarnedFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reports/interest-earned/export/pdf`, { params: toFilterParams(filters), responseType: 'blob' });
  }
}
