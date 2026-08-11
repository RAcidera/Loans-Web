import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { CreateCustomerUseCase } from '../../application/use-cases/create-customer.use-case';

@Component({
  selector: 'lm-add-customer-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './add-customer-dialog.component.html',
  styleUrls: ['./add-customer-dialog.component.scss'],
})
export class AddCustomerDialogComponent {
  submitting = false;

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    fullName: ['', Validators.required],
    address: ['', Validators.required],
    contactNumber: ['', Validators.required],
    borrowerType: ['', Validators.required],
    nicknameAlias: [''],
    notes: [''],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddCustomerDialogComponent>,
    private readonly createCustomer: CreateCustomerUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { fullName, address, contactNumber, borrowerType, nicknameAlias, notes } = this.form.getRawValue();
    this.createCustomer.execute(fullName!, address!, contactNumber!, borrowerType!, nicknameAlias ?? '', notes ?? '').subscribe(() => {
      this.dialogRef.close({ added: true });
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
