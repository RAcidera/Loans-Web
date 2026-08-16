import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';

import { CashLedgerEntry, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { AddCashTransactionUseCase } from '../../application/use-cases/add-cash-transaction.use-case';
import { EditCashTransactionUseCase } from '../../application/use-cases/edit-cash-transaction.use-case';
import { todayLocalDateString } from '../shared/date-utils';

export interface AddCashTransactionDialogData {
  /** Present in edit mode (row menu "Edit") — prefills the form and submits via EditCashTransactionUseCase instead of Add. */
  entry?: CashLedgerEntry;
}

/**
 * Only exposes the manual transaction types an admin would enter by hand
 * from the Transactions page. loan_release and payment_received are
 * system-generated side effects of other actions (disbursing a loan,
 * recording a payment) and are not offered here to avoid double-counting
 * the ledger — see AddCashTransactionCommandHandler on the backend.
 */
const MANUAL_TYPES: Array<{ value: CashTransactionType; label: string }> = [
  { value: 'owner_deposit', label: 'Cash deposit / capital added' },
  { value: 'owner_withdrawal', label: 'Cash withdrawal' },
  { value: 'expense', label: 'Expense' },
  { value: 'adjustment', label: 'Adjustment' },
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
    MatButtonToggleModule,
  ],
  templateUrl: './add-cash-transaction-dialog.component.html',
  styleUrls: ['./add-cash-transaction-dialog.component.scss'],
})
export class AddCashTransactionDialogComponent {
  submitting = false;
  types = MANUAL_TYPES;
  directionRequiredButMissing = false;

  readonly data: AddCashTransactionDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });
  readonly isEditMode = !!this.data?.entry;

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    transactionType: [(this.data?.entry?.transactionType ?? 'owner_deposit') as CashTransactionType, Validators.required],
    amount: [this.data?.entry?.amount ?? 0, [Validators.required, Validators.min(1)]],
    transactionDate: [this.data?.entry?.transactionDate ?? todayLocalDateString(), Validators.required],
    isCashIn: [this.data?.entry?.isCashIn ?? null as boolean | null],
    remarks: [this.data?.entry?.remarks ?? '', Validators.required],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddCashTransactionDialogComponent>,
    private readonly addCashTransaction: AddCashTransactionUseCase,
    private readonly editCashTransaction: EditCashTransactionUseCase,
  ) {}

  get isAdjustment(): boolean {
    return this.form.controls.transactionType.value === 'adjustment';
  }

  submit(): void {
    if (this.form.invalid) return;

    const { transactionType, amount, transactionDate, isCashIn, remarks } = this.form.getRawValue();
    if (transactionType === 'adjustment' && isCashIn === null) {
      this.directionRequiredButMissing = true;
      return;
    }
    this.directionRequiredButMissing = false;

    this.submitting = true;
    const request$ = this.isEditMode
      ? this.editCashTransaction.execute(this.data!.entry!.ledgerId, transactionType!, amount!, remarks!, transactionDate!, isCashIn ?? undefined)
      : this.addCashTransaction.execute(transactionType!, amount!, remarks!, transactionDate!, isCashIn ?? undefined);

    request$.subscribe(() => this.dialogRef.close({ added: true }));
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
