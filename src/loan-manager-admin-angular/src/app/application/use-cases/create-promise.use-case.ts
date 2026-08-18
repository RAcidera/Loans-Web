import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PromiseToPay } from '../../domain/entities/promise-to-pay.entity';
import { PromiseToPayRepository } from '../../domain/repositories/promise-to-pay.repository';

@Injectable({ providedIn: 'root' })
export class CreatePromiseUseCase {
  constructor(private readonly promiseRepository: PromiseToPayRepository) {}

  execute(customerId: string, loanId: string, promiseDate: string, amount: number, notes?: string): Observable<PromiseToPay> {
    return this.promiseRepository.create(customerId, loanId, promiseDate, amount, notes);
  }
}
