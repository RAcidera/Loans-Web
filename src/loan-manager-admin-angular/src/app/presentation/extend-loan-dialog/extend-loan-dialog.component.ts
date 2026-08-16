import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { ExtendLoanUseCase } from '../../application/use-cases/extend-loan.use-case';
import { UpdateExtensionUseCase } from '../../application/use-cases/update-extension.use-case';

export interface ExtendLoanDialogData {
  loanId: string;
  currentDueDate: string;
  /** When set, the dialog edits this existing extension instead of adding a new one. */
  editing?: LoanExtension;
}

/**
 * Shared between "New Extension" and "Edit Extension" — the fields are
 * identical (spec 3.3's extension fields), only the submit action differs.
 */
@Component({
  selector: 'lm-extend-loan-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './extend-loan-dialog.component.html',
  styleUrls: ['./extend-loan-dialog.component.scss'],
})
export class ExtendLoanDialogComponent {
  submitting = false;

  readonly data: ExtendLoanDialogData = inject(MAT_DIALOG_DATA);
  readonly editing = this.data.editing;
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    extensionDays: [this.editing?.extensionDays ?? 30, [Validators.required, Validators.min(1)]],
    additionalChargesAmount: [this.editing?.additionalChargesAmount ?? 0, [Validators.required, Validators.min(0)]],
    remarks: [this.editing?.remarks ?? '', Validators.required],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<ExtendLoanDialogComponent>,
    private readonly extendLoan: ExtendLoanUseCase,
    private readonly updateExtension: UpdateExtensionUseCase,
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { extensionDays, additionalChargesAmount, remarks } = this.form.getRawValue();

    // Kept as two branches (not a ternary feeding one .subscribe()) because
    // updateExtension/extendLoan return different Observable<T> types
    // (LoanExtension vs Loan) — a unioned Observable's .subscribe overloads
    // don't combine cleanly.
    if (this.editing) {
      this.updateExtension
        .execute(this.data.loanId, this.editing.extensionId, extensionDays!, remarks!, additionalChargesAmount!)
        .subscribe(() => this.dialogRef.close({ extended: true }));
    } else {
      this.extendLoan
        .execute(this.data.loanId, extensionDays!, remarks!, additionalChargesAmount!)
        .subscribe(() => this.dialogRef.close({ extended: true }));
    }
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
