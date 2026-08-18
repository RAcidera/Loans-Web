import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Customer } from '../../domain/entities/customer.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { PromiseToPay } from '../../domain/entities/promise-to-pay.entity';
import { CreatePromiseUseCase } from '../../application/use-cases/create-promise.use-case';
import { UpdatePromiseUseCase } from '../../application/use-cases/update-promise.use-case';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';
import { todayLocalDateString } from '../shared/date-utils';

export interface PromiseFormDialogData {
  /** Fixed customer when opened from a page already scoped to one (Loan Details, Customer Profile) — the customer picker is hidden. Omitted when opened context-free (the Calendar's "+ New" menu), in which case a Customer + Loan picker is shown instead. */
  customerId?: string;
  /** Fixed loan when opened from the Loan Details page's Promises tab — the loan picker is hidden. */
  loanId?: string;
  /** Candidate loans for the picker when loanId isn't fixed but customerId is (opened from the Customer Profile's Promises tab). */
  loans?: Loan[];
  editing?: PromiseToPay;
}

/** Requirements §20/§23 — shared between creating and editing a promise-to-pay; editing only changes date/amount/notes, never the customer/loan link or status (those go through the dedicated status-transition actions in promise-list). */
@Component({
  selector: 'lm-promise-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './promise-form-dialog.component.html',
  styleUrls: ['./promise-form-dialog.component.scss'],
})
export class PromiseFormDialogComponent implements OnInit {
  submitting = false;
  error: string | null = null;

  readonly data: PromiseFormDialogData = inject(MAT_DIALOG_DATA) ?? {};
  readonly editing = this.data.editing;
  readonly fixedCustomerId = this.data.customerId;
  readonly fixedLoanId = this.data.loanId;

  /** Context-free mode (opened from the Calendar's "+ New" menu, no customerId/loans given) — the customer/loan pickers load their own full lists rather than relying on a caller-scoped `data.loans`. */
  readonly contextFree = !this.fixedCustomerId && !this.editing;
  customers: Customer[] = [];
  allLoans: Loan[] = [];
  loans: Loan[] = this.data.loans ?? [];

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    customerId: [this.editing?.customerId ?? this.fixedCustomerId ?? '', this.contextFree ? Validators.required : []],
    loanId: [this.editing?.loanId ?? this.fixedLoanId ?? '', Validators.required],
    promiseDate: [this.editing?.promiseDate ?? todayLocalDateString(), Validators.required],
    amount: [this.editing?.amount ?? 0, [Validators.required, Validators.min(1)]],
    notes: [this.editing?.notes ?? ''],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<PromiseFormDialogComponent>,
    private readonly createPromise: CreatePromiseUseCase,
    private readonly updatePromise: UpdatePromiseUseCase,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly getLoans: GetLoansUseCase,
  ) {}

  ngOnInit(): void {
    if (!this.contextFree) return;
    this.getCustomers.execute().subscribe((customers) => (this.customers = customers));
    this.getLoans.execute().subscribe((loans) => {
      this.allLoans = loans;
      this.refreshLoanOptions(this.form.value.customerId ?? '');
    });
  }

  /** Context-free mode only — re-narrows the Loan picker to the chosen customer's loans, since a customer-scoped caller already passes its own pre-filtered `data.loans`. */
  onCustomerChange(customerId: string): void {
    this.refreshLoanOptions(customerId);
    this.form.patchValue({ loanId: '' });
  }

  private refreshLoanOptions(customerId: string): void {
    this.loans = customerId ? this.allLoans.filter((l) => l.customerId === customerId) : [];
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.error = null;
    const { customerId, loanId, promiseDate, amount, notes } = this.form.getRawValue();
    const resolvedCustomerId = this.fixedCustomerId ?? customerId!;

    const result$ = this.editing
      ? this.updatePromise.execute(this.editing.promiseId, promiseDate!, amount!, notes ?? '')
      : this.createPromise.execute(resolvedCustomerId, loanId!, promiseDate!, amount!, notes ?? '');

    result$.subscribe({
      next: () => this.dialogRef.close({ saved: true }),
      error: () => {
        this.submitting = false;
        this.error = 'Could not save this promise. Please try again.';
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
