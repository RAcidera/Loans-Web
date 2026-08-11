import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { CreateUserUseCase } from '../../application/use-cases/create-user.use-case';

@Component({
  selector: 'lm-add-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './add-user-dialog.component.html',
  styleUrls: ['./add-user-dialog.component.scss'],
})
export class AddUserDialogComponent {
  submitting = false;
  errorMessage: string | null = null;

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['staff', Validators.required],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddUserDialogComponent>,
    private readonly createUser: CreateUserUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.errorMessage = null;
    const { username, password, role } = this.form.getRawValue();
    this.createUser.execute(username!, password!, role as 'admin' | 'staff').subscribe({
      next: () => this.dialogRef.close({ added: true }),
      error: (err) => {
        this.submitting = false;
        this.errorMessage = err.status === 400 ? 'Username already taken.' : 'Something went wrong. Please try again.';
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
