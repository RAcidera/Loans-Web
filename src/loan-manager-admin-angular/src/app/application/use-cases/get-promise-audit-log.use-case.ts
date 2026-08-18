import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PromiseAuditLogEntry } from '../../domain/entities/promise-to-pay.entity';
import { PromiseToPayRepository } from '../../domain/repositories/promise-to-pay.repository';

@Injectable({ providedIn: 'root' })
export class GetPromiseAuditLogUseCase {
  constructor(private readonly promiseRepository: PromiseToPayRepository) {}

  execute(promiseId: string): Observable<PromiseAuditLogEntry[]> {
    return this.promiseRepository.getAuditLog(promiseId);
  }
}
