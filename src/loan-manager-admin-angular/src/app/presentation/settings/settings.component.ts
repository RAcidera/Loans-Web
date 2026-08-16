import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AppUser, UserStatus } from '../../domain/entities/app-user.entity';
import { UserRole } from '../../application/auth/auth.service';
import { GetUsersUseCase } from '../../application/use-cases/get-users.use-case';
import { DeactivateUserUseCase } from '../../application/use-cases/deactivate-user.use-case';
import { ActivateUserUseCase } from '../../application/use-cases/activate-user.use-case';
import { ChangeUserRoleUseCase } from '../../application/use-cases/change-user-role.use-case';
import { AddUserDialogComponent } from '../add-user-dialog/add-user-dialog.component';
import { ResetPasswordDialogComponent } from '../reset-password-dialog/reset-password-dialog.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AuthService } from '../../application/auth/auth.service';

type StatusFilter = 'all' | UserStatus;

/**
 * Presentation layer — Phase 3 "User Management": the user list (Admin
 * only, per the plan's "Staff cannot see the user list" acceptance
 * criterion). Self-service "change my password" now lives in the topbar's
 * account menu (ChangeMyPasswordDialogComponent) — this page only ever
 * acts on OTHER users' accounts (activate/deactivate, role, password reset).
 */
@Component({
  selector: 'lm-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit {
  users: AppUser[] = [];
  displayedColumns = ['username', 'role', 'status', 'createdAt', 'actions'];
  usersError: string | null = null;

  statusFilter: StatusFilter = 'all';
  searchTerm = '';

  constructor(
    private readonly getUsers: GetUsersUseCase,
    private readonly deactivateUser: DeactivateUserUseCase,
    private readonly activateUser: ActivateUserUseCase,
    private readonly changeUserRole: ChangeUserRoleUseCase,
    private readonly dialog: MatDialog,
    private readonly confirmDialog: ConfirmDialogService,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    if (this.authService.hasRole('admin')) {
      this.loadUsers();
    }
  }

  private loadUsers(): void {
    this.getUsers.execute().subscribe((users) => (this.users = users));
  }

  get filteredUsers(): AppUser[] {
    const term = this.searchTerm.trim().toLowerCase();
    return this.users.filter((u) => {
      if (this.statusFilter !== 'all' && u.status !== this.statusFilter) return false;
      if (term && !u.username.toLowerCase().includes(term)) return false;
      return true;
    });
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter = filter;
  }

  getInitials(username: string): string {
    const parts = username.replace(/[@.].*$/, '').split(/[\s._-]+/).filter(Boolean);
    const initials = parts.slice(0, 2).map((p) => p[0]).join('');
    return (initials || username.slice(0, 2)).toUpperCase();
  }

  openAddUser(): void {
    this.dialog
      .open(AddUserDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.loadUsers();
      });
  }

  /** Backend also enforces this (400 on self-deactivate) — checked here too so the button itself can be hidden rather than just erroring after the click. */
  isSelf(user: AppUser): boolean {
    return user.username === this.authService.username();
  }

  async deactivate(user: AppUser): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Deactivate user?',
      message: `Deactivate "${user.username}"? They won't be able to log in until reactivated.`,
      confirmText: 'Yes, deactivate',
    });
    if (!ok) return;

    this.usersError = null;
    this.deactivateUser.execute(user.userId).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.usersError = err.status === 400 ? 'You cannot deactivate your own account.' : 'Something went wrong. Please try again.';
      },
    });
  }

  activate(user: AppUser): void {
    this.usersError = null;
    this.activateUser.execute(user.userId).subscribe({
      next: () => this.loadUsers(),
      error: () => {
        this.usersError = 'Something went wrong. Please try again.';
      },
    });
  }

  changeRole(user: AppUser, role: UserRole): void {
    if (role === user.role) return;
    this.usersError = null;
    const previousRole = user.role;
    user.role = role; // optimistic — the mat-select is already bound to it, avoids a visible snap-back on success

    this.changeUserRole.execute(user.userId, role).subscribe({
      error: () => {
        user.role = previousRole;
        this.usersError = 'Something went wrong changing the role. Please try again.';
      },
    });
  }

  openResetPassword(user: AppUser): void {
    this.dialog.open(ResetPasswordDialogComponent, {
      width: '420px',
      maxWidth: '95vw',
      data: { userId: user.userId, username: user.username },
    });
  }
}
