import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ReactiveFormsModule, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { ResetUserPasswordUseCase } from '../../application/use-cases/reset-user-password.use-case';

export interface ResetPasswordDialogData {
  userId: string;
  username: string;
}

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmNewPassword = group.get('confirmNewPassword')?.value;
  return newPassword === confirmNewPassword ? null : { passwordsMismatch: true };
}

/**
 * Admin-only — sets a brand NEW password for another user who forgot
 * theirs. Deliberately has no "current password" field (unlike
 * ChangeMyPasswordDialogComponent) since the admin isn't proving they know
 * the old one; the backend's AdminResetPasswordCommand mirrors that.
 */
@Component({
  selector: 'lm-reset-password-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './reset-password-dialog.component.html',
  styleUrls: ['./reset-password-dialog.component.scss'],
})
export class ResetPasswordDialogComponent {
  submitting = false;
  errorMessage: string | null = null;

  readonly data: ResetPasswordDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required],
    },
    { validators: passwordsMatchValidator },
  );

  constructor(
    private readonly dialogRef: MatDialogRef<ResetPasswordDialogComponent>,
    private readonly resetUserPassword: ResetUserPasswordUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.errorMessage = null;
    const { newPassword } = this.form.getRawValue();

    this.resetUserPassword.execute(this.data.userId, newPassword!).subscribe({
      next: () => this.dialogRef.close({ reset: true }),
      error: () => {
        this.submitting = false;
        this.errorMessage = 'Something went wrong. Please try again.';
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
