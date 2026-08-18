import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

import { Customer } from '../../domain/entities/customer.entity';
import { Loan, LoanClassification, LoanStatus } from '../../domain/entities/loan.entity';

/**
 * Requirements' Advanced Filters (kept behind a "Filters" button per the
 * layout rules — never an always-visible panel). Applied client-side by
 * calendar-page against the already-fetched event set: Loan Status/
 * Classification/Amount Range only affect events with a resolvable Loan
 * (loan_due, extension_due, promise, and any diary/reminder explicitly
 * linked to a loan) — see CalendarEventDto's CustomerId/LoanId doc comment.
 */
export interface CalendarAdvancedFilters {
  customerId?: string;
  loanId?: string;
  loanStatuses?: LoanStatus[];
  classifications?: LoanClassification[];
  borrowerType?: string;
  amountMin?: number;
  amountMax?: number;
}

export interface CalendarAdvancedFiltersDialogData {
  customers: Customer[];
  loans: Loan[];
  current: CalendarAdvancedFilters;
}

@Component({
  selector: 'lm-calendar-advanced-filters-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './calendar-advanced-filters-dialog.component.html',
  styleUrls: ['./calendar-advanced-filters-dialog.component.scss'],
})
export class CalendarAdvancedFiltersDialogComponent {
  readonly data: CalendarAdvancedFiltersDialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly customers = this.data.customers;
  readonly loans = this.data.loans;
  readonly borrowerTypes = Array.from(new Set(this.customers.map((c) => c.borrowerType).filter(Boolean))).sort();

  form = this.fb.group({
    customerId: [this.data.current.customerId ?? ''],
    loanId: [this.data.current.loanId ?? ''],
    loanStatuses: [this.data.current.loanStatuses ?? ([] as LoanStatus[])],
    classifications: [this.data.current.classifications ?? ([] as LoanClassification[])],
    borrowerType: [this.data.current.borrowerType ?? ''],
    amountMin: [this.data.current.amountMin ?? null],
    amountMax: [this.data.current.amountMax ?? null],
  });

  constructor(private readonly dialogRef: MatDialogRef<CalendarAdvancedFiltersDialogComponent, CalendarAdvancedFilters | undefined>) {}

  apply(): void {
    const raw = this.form.getRawValue();
    this.dialogRef.close({
      customerId: raw.customerId || undefined,
      loanId: raw.loanId || undefined,
      loanStatuses: raw.loanStatuses?.length ? raw.loanStatuses : undefined,
      classifications: raw.classifications?.length ? raw.classifications : undefined,
      borrowerType: raw.borrowerType || undefined,
      amountMin: raw.amountMin ?? undefined,
      amountMax: raw.amountMax ?? undefined,
    });
  }

  clear(): void {
    this.dialogRef.close({});
  }

  cancel(): void {
    this.dialogRef.close(undefined);
  }
}
