import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, ReactiveFormsModule, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { ChangePasswordUseCase } from '../../application/use-cases/change-password.use-case';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmNewPassword = group.get('confirmNewPassword')?.value;
  return newPassword === confirmNewPassword ? null : { passwordsMismatch: true };
}

/**
 * Self-service password change — triggered from the topbar's account menu
 * (moved out of the Settings page, which now only handles OTHER users'
 * accounts). Stays open on success and shows a confirmation instead of
 * auto-closing, so the user can see the change actually took effect.
 */
@Component({
  selector: 'lm-change-my-password-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './change-my-password-dialog.component.html',
  styleUrls: ['./change-my-password-dialog.component.scss'],
})
export class ChangeMyPasswordDialogComponent {
  submitting = false;
  success = false;
  errorMessage: string | null = null;

  private readonly fb = inject(FormBuilder);

  form = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required],
    },
    { validators: passwordsMatchValidator },
  );

  constructor(
    private readonly dialogRef: MatDialogRef<ChangeMyPasswordDialogComponent>,
    private readonly changePassword: ChangePasswordUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;

    this.submitting = true;
    this.success = false;
    this.errorMessage = null;
    const { currentPassword, newPassword } = this.form.getRawValue();

    this.changePassword.execute(currentPassword!, newPassword!).subscribe({
      next: () => {
        this.submitting = false;
        this.success = true;
        this.form.reset();
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.status === 401 ? 'Current password is incorrect.' : 'Something went wrong. Please try again.';
      },
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
