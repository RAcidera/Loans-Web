import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AppUser, UserStatus } from '../../domain/entities/app-user.entity';
import { GetUsersUseCase } from '../../application/use-cases/get-users.use-case';
import { ChangePasswordUseCase } from '../../application/use-cases/change-password.use-case';
import { DeactivateUserUseCase } from '../../application/use-cases/deactivate-user.use-case';
import { AddUserDialogComponent } from '../add-user-dialog/add-user-dialog.component';
import { AuthService } from '../../application/auth/auth.service';

type StatusFilter = 'all' | UserStatus;

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmNewPassword = group.get('confirmNewPassword')?.value;
  return newPassword === confirmNewPassword ? null : { passwordsMismatch: true };
}

/**
 * Presentation layer — Phase 3 "User Management": the user list (Admin
 * only, per the plan's "Staff cannot see the user list" acceptance
 * criterion) plus a self-service "change my password" form (both roles).
 */
@Component({
  selector: 'lm-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
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

  passwordSubmitting = false;
  passwordSuccess = false;
  passwordError: string | null = null;

  private readonly fb = new FormBuilder();

  passwordForm: FormGroup = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required],
    },
    { validators: passwordsMatchValidator },
  );

  constructor(
    private readonly getUsers: GetUsersUseCase,
    private readonly changePassword: ChangePasswordUseCase,
    private readonly deactivateUser: DeactivateUserUseCase,
    private readonly dialog: MatDialog,
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

  deactivate(user: AppUser): void {
    this.usersError = null;
    this.deactivateUser.execute(user.userId).subscribe({
      next: () => this.loadUsers(),
      error: (err) => {
        this.usersError = err.status === 400 ? 'You cannot deactivate your own account.' : 'Something went wrong. Please try again.';
      },
    });
  }

  submitPasswordChange(): void {
    if (this.passwordForm.invalid) return;

    this.passwordSubmitting = true;
    this.passwordSuccess = false;
    this.passwordError = null;
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.changePassword.execute(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordSubmitting = false;
        this.passwordSuccess = true;
        this.passwordForm.reset();
      },
      error: (err) => {
        this.passwordSubmitting = false;
        this.passwordError = err.status === 401
          ? 'Current password is incorrect.'
          : 'Something went wrong. Please try again.';
      },
    });
  }
}
