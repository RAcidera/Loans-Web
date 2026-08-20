import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

import { Loan } from '../../domain/entities/loan.entity';
import { PromiseStatus, PromiseToPay } from '../../domain/entities/promise-to-pay.entity';
import { GetPromisesByCustomerUseCase } from '../../application/use-cases/get-promises-by-customer.use-case';
import { GetPromisesByLoanUseCase } from '../../application/use-cases/get-promises-by-loan.use-case';
import { DeletePromiseUseCase } from '../../application/use-cases/delete-promise.use-case';
import { MarkPromiseKeptUseCase } from '../../application/use-cases/mark-promise-kept.use-case';
import { MarkPromiseMissedUseCase } from '../../application/use-cases/mark-promise-missed.use-case';
import { ReschedulePromiseUseCase } from '../../application/use-cases/reschedule-promise.use-case';
import { CancelPromiseUseCase } from '../../application/use-cases/cancel-promise.use-case';
import { PromiseFormDialogComponent } from '../promise-form-dialog/promise-form-dialog.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AppDatePipe } from '../shared/app-date.pipe';

const STATUS_LABEL: Record<PromiseStatus, string> = {
  pending: 'Pending',
  kept: 'Kept',
  missed: 'Missed',
  rescheduled: 'Rescheduled',
  cancelled: 'Cancelled',
};

/**
 * Self-contained "Promises" tab content — embedded on both the Loan
 * Details page (scoped to one loan, loanId fixed) and the Customer Profile
 * page (scoped to a customer across all their loans, loanId omitted) per
 * the implementation plan's Phase 3 assumption: no standalone Promises
 * list page exists, requirements §20's "payments are irregular" framing is
 * a customer/loan-level concern.
 */
@Component({
  selector: 'lm-promise-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatIconModule, MatButtonModule, MatMenuModule, MatDialogModule, AppDatePipe],
  templateUrl: './promise-list.component.html',
  styleUrls: ['./promise-list.component.scss'],
})
export class PromiseListComponent implements OnChanges {
  @Input({ required: true }) customerId!: string;
  @Input() loanId?: string;
  @Input() loans: Loan[] = [];

  promises: PromiseToPay[] = [];
  loading = true;
  statusLabel = STATUS_LABEL;

  reschedulingId: string | null = null;
  rescheduleDate = '';

  constructor(
    private readonly getByCustomer: GetPromisesByCustomerUseCase,
    private readonly getByLoan: GetPromisesByLoanUseCase,
    private readonly deletePromise: DeletePromiseUseCase,
    private readonly markKeptUseCase: MarkPromiseKeptUseCase,
    private readonly markMissedUseCase: MarkPromiseMissedUseCase,
    private readonly reschedulePromiseUseCase: ReschedulePromiseUseCase,
    private readonly cancelPromiseUseCase: CancelPromiseUseCase,
    private readonly dialog: MatDialog,
    private readonly confirmDialog: ConfirmDialogService,
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['customerId'] || changes['loanId']) this.load();
  }

  private load(): void {
    if (!this.customerId) return;
    this.loading = true;
    const request$ = this.loanId ? this.getByLoan.execute(this.loanId) : this.getByCustomer.execute(this.customerId);
    request$.subscribe((promises) => {
      this.promises = promises;
      this.loading = false;
    });
  }

  /** Only a still-active promise (Pending/Rescheduled) can transition further — a Kept/Missed/Cancelled one is history. */
  isActionable(promise: PromiseToPay): boolean {
    return promise.status === 'pending' || promise.status === 'rescheduled';
  }

  openNew(): void {
    this.dialog
      .open(PromiseFormDialogComponent, { width: '480px', maxWidth: '95vw', data: { customerId: this.customerId, loanId: this.loanId, loans: this.loans } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }

  openEdit(promise: PromiseToPay): void {
    this.dialog
      .open(PromiseFormDialogComponent, {
        width: '480px',
        maxWidth: '95vw',
        data: { customerId: this.customerId, loanId: this.loanId, loans: this.loans, editing: promise },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }

  markKept(promise: PromiseToPay): void {
    this.markKeptUseCase.execute(promise.promiseId).subscribe(() => this.load());
  }

  markMissed(promise: PromiseToPay): void {
    this.markMissedUseCase.execute(promise.promiseId).subscribe(() => this.load());
  }

  startReschedule(promise: PromiseToPay): void {
    this.reschedulingId = promise.promiseId;
    this.rescheduleDate = promise.promiseDate;
  }

  confirmReschedule(promise: PromiseToPay): void {
    if (!this.rescheduleDate) return;
    this.reschedulePromiseUseCase.execute(promise.promiseId, this.rescheduleDate).subscribe(() => {
      this.reschedulingId = null;
      this.load();
    });
  }

  cancelReschedule(): void {
    this.reschedulingId = null;
  }

  async cancelPromise(promise: PromiseToPay): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Cancel this promise?',
      message: `Cancel the promise to pay ₱${promise.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })} on ${promise.promiseDate}?`,
      confirmText: 'Yes, cancel',
    });
    if (!ok) return;
    this.cancelPromiseUseCase.execute(promise.promiseId).subscribe(() => this.load());
  }

  async deletePromiseRow(promise: PromiseToPay): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Delete promise?',
      message: 'Delete this promise-to-pay record? This cannot be undone.',
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
    this.deletePromise.execute(promise.promiseId).subscribe(() => this.load());
  }
}
