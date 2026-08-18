import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PromiseToPay } from '../../domain/entities/promise-to-pay.entity';
import { PromiseToPayRepository } from '../../domain/repositories/promise-to-pay.repository';

@Injectable({ providedIn: 'root' })
export class MarkPromiseKeptUseCase {
  constructor(private readonly promiseRepository: PromiseToPayRepository) {}

  execute(promiseId: string): Observable<PromiseToPay> {
    return this.promiseRepository.markKept(promiseId);
  }
}
