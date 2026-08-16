import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Customer } from '../../domain/entities/customer.entity';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { CreateLoanUseCase } from '../../application/use-cases/create-loan.use-case';
import { todayLocalDateString } from '../shared/date-utils';

export interface AddLoanDialogData {
  /** Preselects and locks the customer field — used when "New Loan" is launched from a Customer Profile page. */
  customerId?: string;
  customerName?: string;
}

const DEFAULT_INTEREST_RATE_PERCENT = 7;
const DEFAULT_PAYMENT_TERMS_MONTHS = 2;

function round2(n: number): number {
  return Math.round(n * 100) / 100;
}

/** Payment Terms dropdown — most customers are 2 months (60 days, paid daily); some are weekly/biweekly/monthly, same field, different month count. */
export const PAYMENT_TERMS_MONTHS_OPTIONS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

@Component({
  selector: 'lm-add-loan-dialog',
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
  templateUrl: './add-loan-dialog.component.html',
  styleUrls: ['./add-loan-dialog.component.scss'],
})
export class AddLoanDialogComponent implements OnInit {
  submitting = false;
  customers: Customer[] = [];
  paymentTermsOptions = PAYMENT_TERMS_MONTHS_OPTIONS;

  readonly data: AddLoanDialogData | null = inject(MAT_DIALOG_DATA, { optional: true });
  private readonly fb = inject(FormBuilder);

  /** Stops the Interest Amount auto-calc from overwriting a manual override — set once the lender types into that field directly (its valueChanges only fires on real user edits; our own auto-calc writes use emitEvent: false). */
  private interestAmountTouched = false;

  form = this.fb.group({
    customerId: [this.data?.customerId ?? '', Validators.required],
    principal: [null as number | null, [Validators.required, Validators.min(1)]],
    loanDate: [todayLocalDateString(), Validators.required],
    paymentTermsMonths: [DEFAULT_PAYMENT_TERMS_MONTHS, Validators.required],
    interestRatePercent: [DEFAULT_INTEREST_RATE_PERCENT, [Validators.required, Validators.min(0)]],
    interestAmount: [0, [Validators.required, Validators.min(0)]],
    dailyPayment: [{ value: 0, disabled: true }],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AddLoanDialogComponent>,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly createLoan: CreateLoanUseCase,
  ) {}

  ngOnInit(): void {
    if (!this.data?.customerId) {
      this.getCustomers.execute().subscribe((customers) => (this.customers = customers));
    }

    this.form.controls.principal.valueChanges.subscribe(() => this.recalculate());
    this.form.controls.paymentTermsMonths.valueChanges.subscribe(() => this.recalculate());
    this.form.controls.interestRatePercent.valueChanges.subscribe(() => this.recalculate());
    this.form.controls.interestAmount.valueChanges.subscribe(() => {
      this.interestAmountTouched = true;
      this.recalculateDailyPaymentOnly();
    });

    this.recalculate();
  }

  /** Interest Amount = Principal * Rate * Payment Terms (months), unless manually overridden. */
  private recalculate(): void {
    const { principal, paymentTermsMonths, interestRatePercent } = this.form.getRawValue();

    if (!this.interestAmountTouched) {
      const interestAmount = round2((principal ?? 0) * ((interestRatePercent ?? 0) / 100) * (paymentTermsMonths ?? DEFAULT_PAYMENT_TERMS_MONTHS));
      this.form.controls.interestAmount.setValue(interestAmount, { emitEvent: false });
    }

    this.recalculateDailyPaymentOnly();
  }

  /** Daily Payment = (Principal + Interest Amount) / (Payment Terms * 30) — always computed, never overridden. */
  private recalculateDailyPaymentOnly(): void {
    const { principal, paymentTermsMonths, interestAmount } = this.form.getRawValue();
    const days = Math.max(1, (paymentTermsMonths ?? DEFAULT_PAYMENT_TERMS_MONTHS) * 30);
    const dailyPayment = round2(((principal ?? 0) + (interestAmount ?? 0)) / days);
    this.form.controls.dailyPayment.setValue(dailyPayment, { emitEvent: false });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { customerId, principal, loanDate, paymentTermsMonths, interestRatePercent, interestAmount } = this.form.getRawValue();

    this.createLoan
      .execute(customerId!, principal!, interestRatePercent! / 100, paymentTermsMonths!, loanDate!, interestAmount!)
      .subscribe(() => {
        this.dialogRef.close({ added: true });
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
