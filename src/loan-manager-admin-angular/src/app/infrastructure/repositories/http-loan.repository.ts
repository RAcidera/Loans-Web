import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CustomerListItem, LoanRepository, UpdateLoanFields } from '../../domain/repositories/loan.repository';
import { Customer, CustomerStatus } from '../../domain/entities/customer.entity';
import { CustomerDocument } from '../../domain/entities/customer-document.entity';
import { CustomerPayment } from '../../domain/entities/customer-payment.entity';
import { CustomerTotals } from '../../domain/entities/customer-totals.entity';
import { DashboardReceivables } from '../../domain/entities/dashboard-receivables.entity';
import { DashboardSummary } from '../../domain/entities/dashboard-summary.entity';
import { Loan, LoanClassification, LoanPageFilters } from '../../domain/entities/loan.entity';
import { LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanDocument } from '../../domain/entities/loan-document.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { LoanTotals } from '../../domain/entities/loan-totals.entity';
import { PagedResult } from '../../domain/entities/paged-result.entity';
import { Payment, PaymentMethod, PaymentPageFilters, PaymentWithCustomer, PaymentsTotals } from '../../domain/entities/payment.entity';

/** Flattens LoanPageFilters into HttpClient query params, matching LoansController's GetPage/GetPageTotals query-string names. */
function toFilterParams(filters?: LoanPageFilters): Record<string, string | number | boolean> {
  if (!filters) return {};
  const params: Record<string, string | number | boolean> = {};
  if (filters.statuses?.length) params['status'] = filters.statuses.join(',');
  if (filters.classifications?.length) params['classification'] = filters.classifications.join(',');
  if (filters.loanDateFrom) params['loanDateFrom'] = filters.loanDateFrom;
  if (filters.loanDateTo) params['loanDateTo'] = filters.loanDateTo;
  if (filters.dueDateFrom) params['dueDateFrom'] = filters.dueDateFrom;
  if (filters.dueDateTo) params['dueDateTo'] = filters.dueDateTo;
  if (filters.badLoansOnly) params['badLoansOnly'] = true;
  if (filters.overdueOnly) params['overdueOnly'] = true;
  return params;
}

/** Flattens PaymentPageFilters into HttpClient query params, matching PaymentsController's GetPage/GetPageTotals query-string names. */
function toPaymentFilterParams(filters?: PaymentPageFilters): Record<string, string> {
  if (!filters) return {};
  const params: Record<string, string> = {};
  if (filters.loanSearch) params['loanSearch'] = filters.loanSearch;
  if (filters.customerSearch) params['customerSearch'] = filters.customerSearch;
  if (filters.dateFrom) params['dateFrom'] = filters.dateFrom;
  if (filters.dateTo) params['dateTo'] = filters.dateTo;
  return params;
}

/**
 * Talks to the LoanManagementSystem.Api backend (.NET 8, Clean
 * Architecture + DDD, EF Core, SQL Server). Registered in app.config.ts in
 * place of MockLoanRepository — see that file for the swap point, and its
 * comments for how to switch back to the mock for offline demos.
 */
@Injectable()
export class HttpLoanRepository extends LoanRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(`${this.baseUrl}/customers`);
  }

  getCustomersPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
    status?: CustomerStatus,
  ): Observable<PagedResult<CustomerListItem>> {
    const params: Record<string, string | number> = { pageIndex, pageSize, search };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    if (status) params['status'] = status;
    return this.http.get<PagedResult<CustomerListItem>>(`${this.baseUrl}/customers/page`, { params });
  }

  getCustomersTotals(search: string, status?: CustomerStatus): Observable<CustomerTotals> {
    const params: Record<string, string> = { search };
    if (status) params['status'] = status;
    return this.http.get<CustomerTotals>(`${this.baseUrl}/customers/page/totals`, { params });
  }

  exportCustomers(search: string, status?: CustomerStatus): Observable<Blob> {
    const params: Record<string, string> = { search };
    if (status) params['status'] = status;
    return this.http.get(`${this.baseUrl}/customers/export`, { params, responseType: 'blob' });
  }

  getCustomerById(customerId: string): Observable<Customer | undefined> {
    return this.http.get<Customer>(`${this.baseUrl}/customers/${customerId}`);
  }

  createCustomer(
    fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias?: string, notes?: string,
  ): Observable<Customer> {
    return this.http.post<Customer>(`${this.baseUrl}/customers`, { fullName, address, contactNumber, borrowerType, nicknameAlias, notes });
  }

  updateCustomer(
    customerId: string, fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias?: string, notes?: string,
  ): Observable<Customer> {
    return this.http.put<Customer>(`${this.baseUrl}/customers/${customerId}`, { fullName, address, contactNumber, borrowerType, nicknameAlias, notes });
  }

  getCustomerDocuments(customerId: string): Observable<CustomerDocument[]> {
    return this.http.get<CustomerDocument[]>(`${this.baseUrl}/customers/${customerId}/documents`);
  }

  uploadCustomerDocument(customerId: string, file: File): Observable<CustomerDocument> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<CustomerDocument>(`${this.baseUrl}/customers/${customerId}/documents`, formData);
  }

  downloadCustomerDocument(customerId: string, documentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/customers/${customerId}/documents/${documentId}`, { responseType: 'blob' });
  }

  deleteCustomerDocument(customerId: string, documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/customers/${customerId}/documents/${documentId}`);
  }

  getLoans(): Observable<Loan[]> {
    return this.http.get<Loan[]>(`${this.baseUrl}/loans`);
  }

  getLoansPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
    filters?: LoanPageFilters,
  ): Observable<PagedResult<Loan>> {
    const params: Record<string, string | number | boolean> = { pageIndex, pageSize, search, ...toFilterParams(filters) };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    return this.http.get<PagedResult<Loan>>(`${this.baseUrl}/loans/page`, { params });
  }

  getLoansTotals(search: string, filters?: LoanPageFilters): Observable<LoanTotals> {
    const params: Record<string, string | number | boolean> = { search, ...toFilterParams(filters) };
    return this.http.get<LoanTotals>(`${this.baseUrl}/loans/page/totals`, { params });
  }

  exportLoans(search: string, filters?: LoanPageFilters): Observable<Blob> {
    const params: Record<string, string | number | boolean> = { search, ...toFilterParams(filters) };
    return this.http.get(`${this.baseUrl}/loans/export`, { params, responseType: 'blob' });
  }

  getLoanById(loanId: string): Observable<Loan | undefined> {
    return this.http.get<Loan>(`${this.baseUrl}/loans/${loanId}`);
  }

  getLoansByCustomer(customerId: string): Observable<Loan[]> {
    return this.http.get<Loan[]>(`${this.baseUrl}/customers/${customerId}/loans`);
  }

  getCustomerPaymentsPage(
    customerId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<CustomerPayment>> {
    const params: Record<string, string | number> = { pageIndex, pageSize };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    return this.http.get<PagedResult<CustomerPayment>>(`${this.baseUrl}/customers/${customerId}/payments/page`, { params });
  }

  getDashboardReceivables(): Observable<DashboardReceivables> {
    return this.http.get<DashboardReceivables>(`${this.baseUrl}/dashboard/receivables`);
  }

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard/summary`);
  }

  getCustomerReceivables(customerId: string): Observable<DashboardReceivables> {
    return this.http.get<DashboardReceivables>(`${this.baseUrl}/customers/${customerId}/receivables`);
  }

  createLoan(
    customerId: string, principal: number, interestRate?: number, paymentTermsMonths?: number,
    startDate?: string, interestAmount?: number,
  ): Observable<Loan> {
    return this.http.post<Loan>(`${this.baseUrl}/loans`, { customerId, principal, interestRate, paymentTermsMonths, startDate, interestAmount });
  }

  updateLoan(loanId: string, fields: UpdateLoanFields): Observable<Loan> {
    return this.http.put<Loan>(`${this.baseUrl}/loans/${loanId}`, fields);
  }

  changeLoanClassification(loanId: string, classification: LoanClassification): Observable<Loan> {
    return this.http.put<Loan>(`${this.baseUrl}/loans/${loanId}/classification`, { classification });
  }

  getExtensions(loanId: string): Observable<LoanExtension[]> {
    return this.http.get<LoanExtension[]>(`${this.baseUrl}/loans/${loanId}/extensions`);
  }

  /**
   * The backend's POST /loans/{id}/extensions returns the created
   * LoanExtensionDto, not the updated Loan — but this port's contract
   * (matching MockLoanRepository, and ultimately what ExtendLoanUseCase
   * promises callers) is Observable<Loan>. Chaining a GET keeps that
   * contract true without changing the backend's REST shape (a POST to a
   * sub-resource returning that sub-resource, not its parent, is the more
   * conventional REST response).
   */
  extendLoan(loanId: string, extensionDays: number, remarks: string, additionalChargesAmount = 0): Observable<Loan> {
    return this.http
      .post(`${this.baseUrl}/loans/${loanId}/extensions`, { extensionDays, remarks, additionalChargesAmount })
      .pipe(switchMap(() => this.getLoanById(loanId) as Observable<Loan>));
  }

  updateExtension(
    loanId: string, extensionId: string, extensionDays: number,
    remarks: string, additionalChargesAmount = 0,
  ): Observable<LoanExtension> {
    return this.http.put<LoanExtension>(
      `${this.baseUrl}/loans/${loanId}/extensions/${extensionId}`,
      { extensionDays, remarks, additionalChargesAmount },
    );
  }

  deleteExtension(loanId: string, extensionId: string): Observable<Loan> {
    return this.http.delete<Loan>(`${this.baseUrl}/loans/${loanId}/extensions/${extensionId}`);
  }

  getPayments(loanId: string): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.baseUrl}/loans/${loanId}/payments`);
  }

  getPaymentsPage(
    loanId: string, pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<Payment>> {
    const params: Record<string, string | number> = { pageIndex, pageSize };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    return this.http.get<PagedResult<Payment>>(`${this.baseUrl}/loans/${loanId}/payments/page`, { params });
  }

  getPaymentsListPage(
    pageIndex: number, pageSize: number, sortBy?: string, sortDir?: 'asc' | 'desc', filters?: PaymentPageFilters,
  ): Observable<PagedResult<PaymentWithCustomer>> {
    const params: Record<string, string | number> = { pageIndex, pageSize, ...toPaymentFilterParams(filters) };
    if (sortBy) params['sortBy'] = sortBy;
    if (sortDir) params['sortDir'] = sortDir;
    return this.http.get<PagedResult<PaymentWithCustomer>>(`${this.baseUrl}/payments/page`, { params });
  }

  getPaymentsListTotals(filters?: PaymentPageFilters): Observable<PaymentsTotals> {
    return this.http.get<PaymentsTotals>(`${this.baseUrl}/payments/page/totals`, { params: toPaymentFilterParams(filters) });
  }

  exportPaymentsList(filters?: PaymentPageFilters): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/payments/export`, { params: toPaymentFilterParams(filters), responseType: 'blob' });
  }

  getRecentPayments(limit: number): Observable<PaymentWithCustomer[]> {
    return this.http.get<PaymentWithCustomer[]>(`${this.baseUrl}/payments/recent`, { params: { limit } });
  }

  recordPayment(loanId: string, amountPaid: number, paymentMethod: PaymentMethod, notes: string, referenceNumber?: string, paymentDate?: string): Observable<Payment> {
    return this.http.post<Payment>(`${this.baseUrl}/loans/${loanId}/payments`, { amountPaid, paymentMethod, notes, referenceNumber, paymentDate });
  }

  updatePayment(
    loanId: string, paymentId: string, amountPaid: number, paymentMethod: PaymentMethod,
    notes?: string, referenceNumber?: string, paymentDate?: string,
  ): Observable<Payment> {
    return this.http.put<Payment>(
      `${this.baseUrl}/loans/${loanId}/payments/${paymentId}`,
      { amountPaid, paymentMethod, notes, referenceNumber, paymentDate },
    );
  }

  deletePayment(loanId: string, paymentId: string): Observable<Loan> {
    return this.http.delete<Loan>(`${this.baseUrl}/loans/${loanId}/payments/${paymentId}`);
  }

  getLoanDocuments(loanId: string): Observable<LoanDocument[]> {
    return this.http.get<LoanDocument[]>(`${this.baseUrl}/loans/${loanId}/documents`);
  }

  uploadLoanDocument(loanId: string, file: File): Observable<LoanDocument> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<LoanDocument>(`${this.baseUrl}/loans/${loanId}/documents`, formData);
  }

  downloadLoanDocument(loanId: string, documentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/loans/${loanId}/documents/${documentId}`, { responseType: 'blob' });
  }

  deleteLoanDocument(loanId: string, documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/loans/${loanId}/documents/${documentId}`);
  }

  getLoanLedger(loanId: string): Observable<LoanLedgerEntry[]> {
    return this.http.get<LoanLedgerEntry[]>(`${this.baseUrl}/loans/${loanId}/ledger`);
  }

  getLoanAuditLog(loanId: string): Observable<LoanAuditLogEntry[]> {
    return this.http.get<LoanAuditLogEntry[]>(`${this.baseUrl}/loans/${loanId}/audit-log`);
  }

  downloadLoanSoa(loanId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/loans/${loanId}/soa`, { responseType: 'blob' });
  }
}
