import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '../../domain/entities/app-user.entity';
import { UserRepository } from '../../domain/repositories/user.repository';
import { UserRole } from '../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class ChangeUserRoleUseCase {
  constructor(private readonly userRepository: UserRepository) {}

  execute(userId: string, role: UserRole): Observable<AppUser> {
    return this.userRepository.changeUserRole(userId, role);
  }
}
