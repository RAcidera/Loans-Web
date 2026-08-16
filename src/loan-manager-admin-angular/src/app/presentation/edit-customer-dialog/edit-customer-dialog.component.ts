import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { Customer } from '../../domain/entities/customer.entity';
import { UpdateCustomerUseCase } from '../../application/use-cases/update-customer.use-case';

export interface EditCustomerDialogData {
  customer: Customer;
}

@Component({
  selector: 'lm-edit-customer-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './edit-customer-dialog.component.html',
  styleUrls: ['./edit-customer-dialog.component.scss'],
})
export class EditCustomerDialogComponent {
  submitting = false;

  readonly data: EditCustomerDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    fullName: [this.data.customer.fullName, Validators.required],
    address: [this.data.customer.address, Validators.required],
    contactNumber: [this.data.customer.contactNumber, Validators.required],
    borrowerType: [this.data.customer.borrowerType, Validators.required],
    nicknameAlias: [this.data.customer.nicknameAlias],
    notes: [this.data.customer.notes],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<EditCustomerDialogComponent>,
    private readonly updateCustomer: UpdateCustomerUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { fullName, address, contactNumber, borrowerType, nicknameAlias, notes } = this.form.getRawValue();
    this.updateCustomer
      .execute(this.data.customer.customerId, fullName!, address!, contactNumber!, borrowerType!, nicknameAlias ?? '', notes ?? '')
      .subscribe((updated) => {
        this.submitting = false;
        this.dialogRef.close({ updated: true, customer: updated });
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
