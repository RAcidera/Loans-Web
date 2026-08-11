import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Payment, PaymentMethod } from '../../domain/entities/payment.entity';
import { RecordPaymentUseCase } from '../../application/use-cases/record-payment.use-case';
import { UpdatePaymentUseCase } from '../../application/use-cases/update-payment.use-case';

export interface AddPaymentDialogData {
  loanId: string;
  balance: number;
  /** When set, the dialog edits this existing payment instead of recording a new one. */
  editing?: Payment;
}

/**
 * Shared between "New Payment" and "Edit Payment" — the fields are
 * identical (spec 3.4's payment fields), only the submit action differs.
 */
@Component({
  selector: 'lm-add-payment-dialog',
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
  templateUrl: './add-payment-dialog.component.html',
  styleUrls: ['./add-payment-dialog.component.scss'],
})
export class AddPaymentDialogComponent {
  submitting = false;

  readonly data: AddPaymentDialogData = inject(MAT_DIALOG_DATA);
  readonly editing = this.data.editing;
  private readonly fb = inject(FormBuilder);

  // Editing an existing payment: the amount already counted against the
  // balance, so the ceiling is balance + its own current amount, not just
  // balance (which would make the existing amount itself invalid).
  private readonly maxAmount = this.data.balance + (this.editing?.amountPaid ?? 0);

  form = this.fb.group({
    amountPaid: [this.editing?.amountPaid ?? this.data.balance, [Validators.required, Validators.min(1), Validators.max(this.maxAmount)]],
    paymentMethod: [(this.editing?.paymentMethod ?? 'cash') as PaymentMethod, Validators.required],
    referenceNumber: [this.editing?.referenceNumber ?? ''],
    notes: [this.editing?.notes ?? ''],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddPaymentDialogComponent>,
    private readonly recordPayment: RecordPaymentUseCase,
    private readonly updatePayment: UpdatePaymentUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { amountPaid, paymentMethod, notes, referenceNumber } = this.form.getRawValue();

    const result$ = this.editing
      ? this.updatePayment.execute(this.data.loanId, this.editing.paymentId, amountPaid!, paymentMethod!, notes ?? '', referenceNumber || undefined)
      : this.recordPayment.execute(this.data.loanId, amountPaid!, paymentMethod!, notes ?? '', referenceNumber || undefined);

    result$.subscribe(() => {
      this.dialogRef.close({ recorded: true });
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
