import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '../../domain/entities/app-user.entity';
import { UserRepository } from '../../domain/repositories/user.repository';

@Injectable({ providedIn: 'root' })
export class GetUsersUseCase {
  constructor(private readonly userRepository: UserRepository) {}

  execute(): Observable<AppUser[]> {
    return this.userRepository.getUsers();
  }
}
