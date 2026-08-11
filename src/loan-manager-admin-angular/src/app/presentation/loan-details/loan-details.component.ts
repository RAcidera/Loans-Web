import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { Loan, LoanClassification, LoanStatus } from '../../domain/entities/loan.entity';
import { LoanAuditAction, LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { Payment } from '../../domain/entities/payment.entity';
import { GetLoanDetailUseCase } from '../../application/use-cases/get-loan-detail.use-case';
import { ChangeLoanClassificationUseCase } from '../../application/use-cases/change-loan-classification.use-case';
import { DeletePaymentUseCase } from '../../application/use-cases/delete-payment.use-case';
import { DeleteExtensionUseCase } from '../../application/use-cases/delete-extension.use-case';
import { DownloadLoanSoaUseCase } from '../../application/use-cases/download-loan-soa.use-case';
import { AddPaymentDialogComponent } from '../add-payment-dialog/add-payment-dialog.component';
import { ExtendLoanDialogComponent } from '../extend-loan-dialog/extend-loan-dialog.component';
import { EditLoanDialogComponent } from '../edit-loan-dialog/edit-loan-dialog.component';
import { LoanTimelineComponent } from '../loan-timeline/loan-timeline.component';
import { DocumentManagerComponent } from '../document-manager/document-manager.component';
import { AuthService } from '../../application/auth/auth.service';

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

const CLASSIFICATION_LABEL: Record<LoanClassification, string> = {
  normal: 'Normal',
  watchlist: 'Watch List',
  badloan: 'Bad Loan',
};

const PAYMENT_METHOD_LABEL: Record<Payment['paymentMethod'], string> = {
  cash: 'Cash',
  gcash: 'GCash',
  bank_transfer: 'Bank transfer',
  other: 'Other',
};

const AUDIT_ACTION_LABEL: Record<LoanAuditAction, string> = {
  edited: 'Edited',
  classification_changed: 'Classification changed',
  written_off: 'Written off',
};

/**
 * Presentation layer — spec's routed Loan Details page, replacing
 * loan-details-dialog as the primary entry point ("Do not display loan
 * details in a popup because payment and extension history can become
 * large"). Overview/Payments/Extensions/Documents/Audit Log are all fully
 * wired here.
 */
@Component({
  selector: 'lm-loan-details',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTabsModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    LoanTimelineComponent,
    DocumentManagerComponent,
  ],
  templateUrl: './loan-details.component.html',
  styleUrls: ['./loan-details.component.scss'],
})
export class LoanDetailsComponent implements OnInit {
  loan: Loan | null = null;
  extensions: LoanExtension[] = [];
  payments: Payment[] = [];
  ledger: LoanLedgerEntry[] = [];
  auditLog: LoanAuditLogEntry[] = [];
  loading = true;
  notFound = false;
  generatingSoa = false;

  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;
  paymentMethodLabel = PAYMENT_METHOD_LABEL;
  auditActionLabel = AUDIT_ACTION_LABEL;

  paymentColumns = ['date', 'amount', 'method', 'balance', 'reference', 'notes', 'actions'];
  paymentFooterColumns = ['footerLabel', 'footerAmount', 'footerMethod', 'footerReference', 'footerNotes', 'footerActions'];
  extensionColumns = ['date', 'days', 'interest', 'charges', 'balance', 'remarks', 'actions'];
  extensionFooterColumns = ['footerLabel', 'footerDays', 'footerInterest', 'footerCharges', 'footerRemarks', 'footerActions'];
  auditLogColumns = ['occurredAt', 'action', 'description', 'performedBy'];

  private readonly route = inject(ActivatedRoute);
  private loanId!: string;

  constructor(
    private readonly router: Router,
    private readonly dialog: MatDialog,
    private readonly getLoanDetail: GetLoanDetailUseCase,
    private readonly changeClassification: ChangeLoanClassificationUseCase,
    private readonly deletePayment: DeletePaymentUseCase,
    private readonly deleteExtension: DeleteExtensionUseCase,
    private readonly downloadLoanSoa: DownloadLoanSoaUseCase,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.loanId = this.route.snapshot.paramMap.get('id')!;
    this.load(true);
  }

  /**
   * `initial` only shows the loading spinner (and its *ngIf swap) on the
   * very first fetch — reloading after an edit/delete keeps the tab group
   * mounted so the selected tab and scroll position survive the refresh
   * instead of snapping back to Overview.
   */
  private load(initial = false): void {
    if (initial) this.loading = true;
    this.getLoanDetail.execute(this.loanId).subscribe(({ loan, extensions, payments, ledger, auditLog }) => {
      this.loan = loan ?? null;
      this.notFound = !loan;
      this.extensions = extensions;
      this.payments = payments;
      this.ledger = ledger;
      this.auditLog = auditLog;
      this.loading = false;
    });
  }

  /** The ledger row for this payment, matched by ReferenceId — null if the ledger hasn't caught up yet (shouldn't normally happen). */
  getPaymentRunningBalance(payment: Payment): number | null {
    return this.ledger.find((e) => e.referenceId === payment.paymentId)?.runningBalance ?? null;
  }

  getExtensionRunningBalance(extension: LoanExtension): number | null {
    return this.ledger.find((e) => e.referenceId === extension.extensionId)?.runningBalance ?? null;
  }

  getAuditActionLabel(action: LoanAuditAction): string {
    return this.auditActionLabel[action];
  }

  generateSoa(): void {
    if (!this.loan || this.generatingSoa) return;
    this.generatingSoa = true;
    const loanNumber = this.loan.loanNumber;
    this.downloadLoanSoa.execute(this.loan.loanId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `SOA-${loanNumber}.pdf`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 30_000);
        this.generatingSoa = false;
      },
      error: () => {
        this.generatingSoa = false;
      },
    });
  }

  get totalPaymentsAmount(): number {
    return this.payments.reduce((sum, p) => sum + p.amountPaid, 0);
  }

  get totalExtensionInterest(): number {
    return this.extensions.reduce((sum, e) => sum + e.additionalInterestAmount, 0);
  }

  get totalExtensionCharges(): number {
    return this.extensions.reduce((sum, e) => sum + e.additionalChargesAmount, 0);
  }

  back(): void {
    this.router.navigate(['/loans']);
  }

  openEditLoan(): void {
    if (!this.loan) return;
    this.dialog
      .open(EditLoanDialogComponent, { width: '480px', maxWidth: '95vw', data: { loan: this.loan } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.edited) this.load();
      });
  }

  openAddPayment(): void {
    if (!this.loan) return;
    this.dialog
      .open(AddPaymentDialogComponent, { width: '420px', maxWidth: '95vw', data: { loanId: this.loan.loanId, balance: this.loan.balance } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.recorded) this.load();
      });
  }

  openEditPayment(payment: Payment): void {
    if (!this.loan) return;
    this.dialog
      .open(AddPaymentDialogComponent, { width: '420px', maxWidth: '95vw', data: { loanId: this.loan.loanId, balance: this.loan.balance, editing: payment } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.recorded) this.load();
      });
  }

  confirmDeletePayment(payment: Payment): void {
    if (!this.loan) return;
    if (!confirm(`Delete this payment of ₱${payment.amountPaid.toLocaleString()}?`)) return;
    this.deletePayment.execute(this.loan.loanId, payment.paymentId).subscribe(() => this.load());
  }

  openExtendLoan(): void {
    if (!this.loan) return;
    this.dialog
      .open(ExtendLoanDialogComponent, { width: '420px', maxWidth: '95vw', data: { loanId: this.loan.loanId, currentDueDate: this.loan.dueDate } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.extended) this.load();
      });
  }

  openEditExtension(extension: LoanExtension): void {
    if (!this.loan) return;
    this.dialog
      .open(ExtendLoanDialogComponent, {
        width: '420px',
        maxWidth: '95vw',
        data: { loanId: this.loan.loanId, currentDueDate: this.loan.dueDate, editing: extension },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result?.extended) this.load();
      });
  }

  confirmDeleteExtension(extension: LoanExtension): void {
    if (!this.loan) return;
    if (!confirm(`Delete this ${extension.extensionDays}-day extension? The due date and charges will revert.`)) return;
    this.deleteExtension.execute(this.loan.loanId, extension.extensionId).subscribe(() => this.load());
  }

  setClassification(classification: LoanClassification): void {
    if (!this.loan) return;
    this.changeClassification.execute(this.loan.loanId, classification).subscribe(() => this.load());
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  getClassificationLabel(classification: LoanClassification): string {
    return this.classificationLabel[classification];
  }

  getPaymentMethodLabel(method: Payment['paymentMethod']): string {
    return this.paymentMethodLabel[method];
  }
}
