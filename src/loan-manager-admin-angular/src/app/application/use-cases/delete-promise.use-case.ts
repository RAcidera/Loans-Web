import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PromiseToPayRepository } from '../../domain/repositories/promise-to-pay.repository';

@Injectable({ providedIn: 'root' })
export class DeletePromiseUseCase {
  constructor(private readonly promiseRepository: PromiseToPayRepository) {}

  execute(promiseId: string): Observable<void> {
    return this.promiseRepository.delete(promiseId);
  }
}
