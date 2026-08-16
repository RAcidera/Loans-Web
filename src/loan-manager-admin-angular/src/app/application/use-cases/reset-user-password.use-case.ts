import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserRepository } from '../../domain/repositories/user.repository';

/** Admin sets a new password for another user who forgot theirs — Settings page's "Reset password" action. */
@Injectable({ providedIn: 'root' })
export class ResetUserPasswordUseCase {
  constructor(private readonly userRepository: UserRepository) {}

  execute(userId: string, newPassword: string): Observable<void> {
    return this.userRepository.resetUserPassword(userId, newPassword);
  }
}
