import { Observable } from 'rxjs';
import { AppUser } from '../entities/app-user.entity';
import { UserRole } from '../../application/auth/auth.service';

/**
 * Port covering the Users table (user management + self-service password
 * change). Kept separate from LoanRepository since users are their own
 * lifecycle boundary, distinct from Customers/Loans/Payments — mirrors how
 * CashLedgerRepository/ReportRepository each get their own dedicated port.
 */
export abstract class UserRepository {
  abstract getUsers(): Observable<AppUser[]>;
  abstract createUser(username: string, password: string, role: UserRole): Observable<AppUser>;
  abstract changeMyPassword(currentPassword: string, newPassword: string): Observable<void>;
  abstract deactivateUser(userId: string): Observable<void>;
  /** Reverses a deactivation — the Settings page's "Reactivate" action. */
  abstract activateUser(userId: string): Observable<void>;
  /** Switches a user between 'staff' and 'admin'. */
  abstract changeUserRole(userId: string, role: UserRole): Observable<AppUser>;
  /** Admin sets a NEW password for another user who forgot theirs — no current-password check, unlike changeMyPassword. */
  abstract resetUserPassword(userId: string, newPassword: string): Observable<void>;
}
