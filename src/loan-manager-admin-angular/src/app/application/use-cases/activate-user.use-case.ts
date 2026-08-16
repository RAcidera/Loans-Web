import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserRepository } from '../../domain/repositories/user.repository';

@Injectable({ providedIn: 'root' })
export class ActivateUserUseCase {
  constructor(private readonly userRepository: UserRepository) {}

  execute(userId: string): Observable<void> {
    return this.userRepository.activateUser(userId);
  }
}
