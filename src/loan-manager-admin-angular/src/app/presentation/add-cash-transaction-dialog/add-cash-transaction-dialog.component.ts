import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { AddCashTransactionUseCase } from '../../application/use-cases/add-cash-transaction.use-case';

/**
 * Only exposes the two manual transaction types an admin would enter by
 * hand from this page (SRS wireframe 0: "Add Cash Transaction button").
 * loan_release and payment_received are system-generated side effects of
 * other actions (disbursing a loan, recording a payment) and are not
 * offered here to avoid double-counting the ledger.
 */
const MANUAL_TYPES: Array<{ value: CashTransactionType; label: string }> = [
  { value: 'owner_deposit', label: 'Owner deposit (cash in)' },
  { value: 'owner_withdrawal', label: 'Owner withdrawal (cash out)' },
  { value: 'expense', label: 'Expense (cash out)' },
];

@Component({
  selector: 'lm-add-cash-transaction-dialog',
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
  templateUrl: './add-cash-transaction-dialog.component.html',
  styleUrls: ['./add-cash-transaction-dialog.component.scss'],
})
export class AddCashTransactionDialogComponent {
  submitting = false;
  types = MANUAL_TYPES;

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    transactionType: ['owner_deposit' as CashTransactionType, Validators.required],
    amount: [0, [Validators.required, Validators.min(1)]],
    remarks: ['', Validators.required],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddCashTransactionDialogComponent>,
    private readonly addCashTransaction: AddCashTransactionUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { transactionType, amount, remarks } = this.form.getRawValue();
    this.addCashTransaction.execute(transactionType!, amount!, remarks!).subscribe(() => {
      this.dialogRef.close({ added: true });
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
