import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserRepository } from '../../domain/repositories/user.repository';

@Injectable({ providedIn: 'root' })
export class ChangePasswordUseCase {
  constructor(private readonly userRepository: UserRepository) {}

  execute(currentPassword: string, newPassword: string): Observable<void> {
    return this.userRepository.changeMyPassword(currentPassword, newPassword);
  }
}
