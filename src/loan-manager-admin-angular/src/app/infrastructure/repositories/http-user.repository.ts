import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserRepository } from '../../domain/repositories/user.repository';
import { AppUser } from '../../domain/entities/app-user.entity';
import { UserRole } from '../../application/auth/auth.service';

/**
 * Talks to UsersController on the .NET backend. Registered in
 * app.config.ts in place of the abstract UserRepository port.
 */
@Injectable()
export class HttpUserRepository extends UserRepository {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {
    super();
  }

  getUsers(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(`${this.baseUrl}/users`);
  }

  createUser(username: string, password: string, role: UserRole): Observable<AppUser> {
    return this.http.post<AppUser>(`${this.baseUrl}/users`, { username, password, role });
  }

  changeMyPassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/users/me/password`, { currentPassword, newPassword });
  }

  deactivateUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/deactivate`, {});
  }
}
