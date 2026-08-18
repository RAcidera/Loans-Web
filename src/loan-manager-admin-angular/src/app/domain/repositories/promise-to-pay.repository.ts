import { Observable } from 'rxjs';
import { PromiseAuditLogEntry, PromiseToPay } from '../entities/promise-to-pay.entity';

/** Port covering the Promise-to-Pay module (requirements §20) — its own lifecycle boundary, separate from Loans/Diary. */
export abstract class PromiseToPayRepository {
  /** Backs the Customer Profile "Promises" tab. */
  abstract getByCustomer(customerId: string): Observable<PromiseToPay[]>;

  /** Backs the Loan Details "Promises" tab. */
  abstract getByLoan(loanId: string): Observable<PromiseToPay[]>;

  abstract getById(promiseId: string): Observable<PromiseToPay>;

  abstract create(customerId: string, loanId: string, promiseDate: string, amount: number, notes?: string): Observable<PromiseToPay>;

  abstract update(promiseId: string, promiseDate: string, amount: number, notes?: string): Observable<PromiseToPay>;

  abstract delete(promiseId: string): Observable<void>;

  abstract markKept(promiseId: string): Observable<PromiseToPay>;

  abstract markMissed(promiseId: string): Observable<PromiseToPay>;

  abstract reschedule(promiseId: string, newPromiseDate: string): Observable<PromiseToPay>;

  abstract cancel(promiseId: string): Observable<PromiseToPay>;

  abstract getAuditLog(promiseId: string): Observable<PromiseAuditLogEntry[]>;
}
