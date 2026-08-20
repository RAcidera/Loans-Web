import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BusinessTimeService } from '../business-time.service';

export type UserRole = 'admin' | 'staff';

interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  username: string;
  role: UserRole;
}

interface StoredSession {
  token: string;
  expiresAtUtc: string;
  username: string;
  role: UserRole;
}

const STORAGE_KEY = 'lms_session';

/**
 * Holds the current login session and talks to POST /api/auth/login.
 * Sits in the application layer conceptually — it's session/use-case state,
 * not a UI concern — but is a plain Angular service rather than a
 * "use case" class since it also needs to hold ongoing state (the current
 * user), not just execute a one-shot operation.
 *
 * Token is kept in localStorage so a page refresh doesn't log the user
 * out — standard practice for a real deployed app (this is not the
 * artifacts-sandbox context where localStorage is unavailable).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly username = signal<string | null>(null);
  readonly role = signal<UserRole | null>(null);

  constructor(private readonly http: HttpClient, private readonly businessTimeService: BusinessTimeService) {
    this.restoreSession();
  }

  login(usernameValue: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, {
      username: usernameValue,
      password,
    }).pipe(
      tap((response) => {
        const session: StoredSession = {
          token: response.token,
          expiresAtUtc: response.expiresAtUtc,
          username: response.username,
          role: response.role,
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
        this.username.set(response.username);
        this.role.set(response.role);
        // BusinessTimeService's own startup fetch ran before login (no
        // token yet, so it 401'd and fell back to the hardcoded default) —
        // refresh now that a valid token exists.
        this.businessTimeService.refresh();
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.username.set(null);
    this.role.set(null);
  }

  getToken(): string | null {
    return this.readSession()?.token ?? null;
  }

  isAuthenticated(): boolean {
    const session = this.readSession();
    if (!session) return false;
    return new Date(session.expiresAtUtc).getTime() > Date.now();
  }

  hasRole(...roles: UserRole[]): boolean {
    const current = this.role();
    return current !== null && roles.includes(current);
  }

  /** Rehydrates the in-memory signals from localStorage on app startup (e.g. after a page refresh). */
  private restoreSession(): void {
    const session = this.readSession();
    if (session && new Date(session.expiresAtUtc).getTime() > Date.now()) {
      this.username.set(session.username);
      this.role.set(session.role);
    } else if (session) {
      // Expired — clean up rather than leave a dead session sitting in storage.
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  private readSession(): StoredSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  }
}
