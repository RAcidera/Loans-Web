import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Loan } from '../../domain/entities/loan.entity';
import { UpdateLoanUseCase } from '../../application/use-cases/update-loan.use-case';
import { PAYMENT_TERMS_MONTHS_OPTIONS } from '../add-loan-dialog/add-loan-dialog.component';

export interface EditLoanDialogData {
  loan: Loan;
}

function round2(n: number): number {
  return Math.round(n * 100) / 100;
}

/**
 * Spec's "Edit Loan" button — overrides Principal/Loan Date/Due Date/
 * Payment Terms/Interest Rate/Interest Amount/Remarks post-creation
 * ("there are cases where customers pay early and the lender may provide
 * a goodwill discount by reducing interest"). Interest Rate is shown/edited
 * as a percentage (e.g. 3, not 0.03) for readability, converted back to a
 * fraction on submit. Interest Amount stays a pure manual override here
 * (unlike Add Loan's auto-calc) — an existing loan's TotalInterest may
 * already include extension interest the simple Principal*Rate*Terms
 * formula doesn't know about, so recomputing it from scratch on this
 * screen would silently drop that. Daily Payment is still live-recomputed
 * from whatever Principal/Interest Amount/Payment Terms end up as, since
 * that combination is always well-defined regardless of how Interest
 * Amount was derived. Changing Principal or Loan Date also revises the
 * mirrored cash_ledger `loan_release` entry — see the backend's
 * LoanOriginationEditedDomainEvent.
 */
@Component({
  selector: 'lm-edit-loan-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './edit-loan-dialog.component.html',
  styleUrls: ['./edit-loan-dialog.component.scss'],
})
export class EditLoanDialogComponent implements OnInit {
  submitting = false;
  paymentTermsOptions = PAYMENT_TERMS_MONTHS_OPTIONS;

  readonly data: EditLoanDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    principal: [this.data.loan.principalAmount, [Validators.required, Validators.min(1)]],
    startDate: [this.data.loan.startDate, Validators.required],
    dueDate: [this.data.loan.dueDate, Validators.required],
    paymentTermsMonths: [this.data.loan.paymentTermsMonths, Validators.required],
    interestRatePercent: [round2(this.data.loan.interestRate * 100), [Validators.required, Validators.min(0)]],
    interestAmount: [this.data.loan.totalInterest, [Validators.required, Validators.min(0)]],
    dailyPayment: [{ value: this.data.loan.dailyPayment, disabled: true }],
    remarks: [this.data.loan.remarks],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<EditLoanDialogComponent>,
    private readonly updateLoan: UpdateLoanUseCase,
  ) {}

  ngOnInit(): void {
    this.form.controls.principal.valueChanges.subscribe(() => this.recalculateDailyPayment());
    this.form.controls.paymentTermsMonths.valueChanges.subscribe(() => this.recalculateDailyPayment());
    this.form.controls.interestAmount.valueChanges.subscribe(() => this.recalculateDailyPayment());
  }

  private recalculateDailyPayment(): void {
    const { principal, paymentTermsMonths, interestAmount } = this.form.getRawValue();
    const days = Math.max(1, (paymentTermsMonths ?? 1) * 30);
    const dailyPayment = round2(((principal ?? 0) + (interestAmount ?? 0)) / days);
    this.form.controls.dailyPayment.setValue(dailyPayment, { emitEvent: false });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { principal, startDate, dueDate, paymentTermsMonths, interestRatePercent, interestAmount, remarks } = this.form.getRawValue();

    this.updateLoan
      .execute(this.data.loan.loanId, {
        principal: principal!,
        startDate: startDate!,
        dueDate: dueDate!,
        paymentTermsMonths: paymentTermsMonths!,
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
