import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { AppUser, UserStatus } from '../../domain/entities/app-user.entity';
import { UserRole } from '../../application/auth/auth.service';
import { GetUsersUseCase } from '../../application/use-cases/get-users.use-case';
import { DeactivateUserUseCase } from '../../application/use-cases/deactivate-user.use-case';
import { ActivateUserUseCase } from '../../application/use-cases/activate-user.use-case';
import { ChangeUserRoleUseCase } from '../../application/use-cases/change-user-role.use-case';
import { GetSettingsUseCase } from '../../application/use-cases/get-settings.use-case';
import { UpdateBusinessTimeZoneUseCase } from '../../application/use-cases/update-business-time-zone.use-case';
import { BusinessTimeService } from '../../application/business-time.service';
import { AddUserDialogComponent } from '../add-user-dialog/add-user-dialog.component';
import { ResetPasswordDialogComponent } from '../reset-password-dialog/reset-password-dialog.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AuthService } from '../../application/auth/auth.service';
import { AppDateTimePipe } from '../shared/app-date-time.pipe';

/** A modest curated fallback for browsers without Intl.supportedValuesOf('timeZone') (Safari < 17, older Firefox) — every entry here IS supported by the same TimeZoneInfo.FindSystemTimeZoneById the backend validates against. */
const FALLBACK_TIME_ZONES = [
  'Asia/Manila', 'Asia/Singapore', 'Asia/Hong_Kong', 'Asia/Tokyo', 'Asia/Shanghai', 'Asia/Kolkata', 'Asia/Dubai',
  'Australia/Sydney', 'Pacific/Auckland',
  'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
  'Europe/London', 'Europe/Paris', 'Europe/Berlin', 'UTC',
];

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
    MatFormFieldModule,
    MatTooltipModule,
    MatDialogModule,
    AppDateTimePipe,
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

  timeZoneOptions: string[] = FALLBACK_TIME_ZONES;
  businessTimeZoneId = '';
  savedBusinessTimeZoneId = '';
  savingSettings = false;
  settingsError: string | null = null;

  constructor(
    private readonly getUsers: GetUsersUseCase,
    private readonly deactivateUser: DeactivateUserUseCase,
    private readonly activateUser: ActivateUserUseCase,
    private readonly changeUserRole: ChangeUserRoleUseCase,
    private readonly getSettings: GetSettingsUseCase,
    private readonly updateBusinessTimeZone: UpdateBusinessTimeZoneUseCase,
    private readonly businessTimeService: BusinessTimeService,
    private readonly dialog: MatDialog,
    private readonly confirmDialog: ConfirmDialogService,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    if (this.authService.hasRole('admin')) {
      this.loadUsers();
      this.loadSettings();
    }
  }

  private loadSettings(): void {
    // Intl.supportedValuesOf is unavailable on some older browsers (Safari < 17) —
    // fall back to the curated list already set as the field's default.
    const supportedValuesOf = (Intl as { supportedValuesOf?: (key: string) => string[] }).supportedValuesOf;
    if (supportedValuesOf) {
      try {
        this.timeZoneOptions = supportedValuesOf('timeZone');
      } catch {
        // Keep the fallback list.
      }
    }

    this.getSettings.execute().subscribe({
      next: (settings) => {
        this.businessTimeZoneId = settings.businessTimeZoneId;
        this.savedBusinessTimeZoneId = settings.businessTimeZoneId;
        // The current value might not be in the curated fallback list (e.g.
        // an admin picked it back when Intl.supportedValuesOf was available)
        // — make sure the select always has something to show as selected.
        if (!this.timeZoneOptions.includes(settings.businessTimeZoneId)) {
          this.timeZoneOptions = [settings.businessTimeZoneId, ...this.timeZoneOptions];
        }
      },
      error: () => {
        this.settingsError = 'Could not load settings. Please try again.';
      },
    });
  }

  saveBusinessTimeZone(): void {
    if (this.businessTimeZoneId === this.savedBusinessTimeZoneId) return;

    this.settingsError = null;
    this.savingSettings = true;
    this.updateBusinessTimeZone.execute(this.businessTimeZoneId).subscribe({
      next: (settings) => {
        this.savingSettings = false;
        this.savedBusinessTimeZoneId = settings.businessTimeZoneId;
        this.businessTimeZoneId = settings.businessTimeZoneId;
        this.businessTimeService.refresh();
      },
      error: () => {
        this.savingSettings = false;
        this.settingsError = 'Something went wrong saving the Business Time Zone. Please try again.';
      },
    });
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
