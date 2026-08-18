import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PromiseToPayRepository } from '../../domain/repositories/promise-to-pay.repository';
import { PromiseAuditLogEntry, PromiseToPay } from '../../domain/entities/promise-to-pay.entity';

/** Talks to LoanManagementSystem.Api's PromisesToPayController. Registered in app.config.ts in place of an abstract PromiseToPayRepository. */
@Injectable()
export class HttpPromiseToPayRepository extends PromiseToPayRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getByCustomer(customerId: string): Observable<PromiseToPay[]> {
    return this.http.get<PromiseToPay[]>(`${this.baseUrl}/promises-to-pay`, { params: { customerId } });
  }

  getByLoan(loanId: string): Observable<PromiseToPay[]> {
    return this.http.get<PromiseToPay[]>(`${this.baseUrl}/promises-to-pay`, { params: { loanId } });
  }

  getById(promiseId: string): Observable<PromiseToPay> {
    return this.http.get<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}`);
  }

  create(customerId: string, loanId: string, promiseDate: string, amount: number, notes?: string): Observable<PromiseToPay> {
    return this.http.post<PromiseToPay>(`${this.baseUrl}/promises-to-pay`, { customerId, loanId, promiseDate, amount, notes });
  }

  update(promiseId: string, promiseDate: string, amount: number, notes?: string): Observable<PromiseToPay> {
    return this.http.put<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}`, { promiseDate, amount, notes });
  }

  delete(promiseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/promises-to-pay/${promiseId}`);
  }

  markKept(promiseId: string): Observable<PromiseToPay> {
    return this.http.post<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}/kept`, {});
  }

  markMissed(promiseId: string): Observable<PromiseToPay> {
    return this.http.post<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}/missed`, {});
  }

  reschedule(promiseId: string, newPromiseDate: string): Observable<PromiseToPay> {
    return this.http.post<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}/reschedule`, { newPromiseDate });
  }

  cancel(promiseId: string): Observable<PromiseToPay> {
    return this.http.post<PromiseToPay>(`${this.baseUrl}/promises-to-pay/${promiseId}/cancel`, {});
  }

  getAuditLog(promiseId: string): Observable<PromiseAuditLogEntry[]> {
    return this.http.get<PromiseAuditLogEntry[]>(`${this.baseUrl}/promises-to-pay/${promiseId}/audit-log`);
  }
}
