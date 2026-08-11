import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { Loan } from '../../domain/entities/loan.entity';
import { UpdateLoanUseCase } from '../../application/use-cases/update-loan.use-case';

export interface EditLoanDialogData {
  loan: Loan;
}

/**
 * Spec's "Edit Loan" button — overrides Loan Date/Due Date/Interest
 * Rate/Interest Amount/Remarks post-creation ("there are cases where
 * customers pay early and the lender may provide a goodwill discount by
 * reducing interest"). Interest Rate is shown/edited as a percentage
 * (e.g. 3, not 0.03) for readability, converted back to a fraction on
 * submit.
 */
@Component({
  selector: 'lm-edit-loan-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './edit-loan-dialog.component.html',
  styleUrls: ['./edit-loan-dialog.component.scss'],
})
export class EditLoanDialogComponent {
  submitting = false;

  readonly data: EditLoanDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    startDate: [this.data.loan.startDate, Validators.required],
    dueDate: [this.data.loan.dueDate, Validators.required],
    interestRatePercent: [this.data.loan.interestRate * 100, [Validators.required, Validators.min(0)]],
    interestAmount: [this.data.loan.totalInterest, [Validators.required, Validators.min(0)]],
    remarks: [this.data.loan.remarks],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<EditLoanDialogComponent>,
    private readonly updateLoan: UpdateLoanUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { startDate, dueDate, interestRatePercent, interestAmount, remarks } = this.form.getRawValue();

    this.updateLoan
      .execute(this.data.loan.loanId, {
        startDate: startDate!,
        dueDate: dueDate!,
        interestRate: interestRatePercent! / 100,
        interestAmount: interestAmount!,
        remarks: remarks ?? '',
      })
      .subscribe(() => {
        this.dialogRef.close({ edited: true });
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
