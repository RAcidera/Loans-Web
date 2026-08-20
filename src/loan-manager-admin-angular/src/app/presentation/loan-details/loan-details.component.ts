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
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';

import { Loan, LoanClassification, LoanStatus } from '../../domain/entities/loan.entity';
import { LoanAuditAction, LoanAuditLogEntry } from '../../domain/entities/loan-audit-log-entry.entity';
import { LoanExtension } from '../../domain/entities/loan-extension.entity';
import { LoanLedgerEntry } from '../../domain/entities/loan-ledger-entry.entity';
import { Payment } from '../../domain/entities/payment.entity';
import { GetLoanDetailUseCase } from '../../application/use-cases/get-loan-detail.use-case';
import { GetLoanPaymentsPageUseCase } from '../../application/use-cases/get-loan-payments-page.use-case';
import { ChangeLoanClassificationUseCase } from '../../application/use-cases/change-loan-classification.use-case';
import { DeletePaymentUseCase } from '../../application/use-cases/delete-payment.use-case';
import { DeleteExtensionUseCase } from '../../application/use-cases/delete-extension.use-case';
import { DownloadLoanSoaUseCase } from '../../application/use-cases/download-loan-soa.use-case';
import { AddPaymentDialogComponent } from '../add-payment-dialog/add-payment-dialog.component';
import { ExtendLoanDialogComponent } from '../extend-loan-dialog/extend-loan-dialog.component';
import { EditLoanDialogComponent } from '../edit-loan-dialog/edit-loan-dialog.component';
import { LoanTimelineComponent } from '../loan-timeline/loan-timeline.component';
import { DocumentManagerComponent } from '../document-manager/document-manager.component';
import { PromiseListComponent } from '../promise-list/promise-list.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AuthService } from '../../application/auth/auth.service';
import { AppDateTimePipe } from '../shared/app-date-time.pipe';
import { AppDatePipe } from '../shared/app-date.pipe';

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

/** Days-until-due within which an active/extended loan is flagged "due soon" in the Financial Summary status message — matches the dashboard's "due this week" convention. */
const DUE_SOON_DAYS = 7;

function fmtMoney(n: number): string {
  return `₱${n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

interface StatusMessage {
  tone: 'good' | 'neutral' | 'warn' | 'danger';
  icon: string;
  text: string;
}

interface TimelineEvent {
  icon: string;
  tone: 'good' | 'neutral' | 'info';
  title: string;
  subtitle: string;
  /** A mix of business dates (payment.paymentDate) and true timestamps (loan.createdAt, auditLog.occurredAt) — see customer-profile.component.ts's ActivityItem.date doc comment for why this is rendered with `appDateTime` for all of them. */
  date: string;
}

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
    MatPaginatorModule,
    MatSortModule,
    LoanTimelineComponent,
    DocumentManagerComponent,
    PromiseListComponent,
    AppDateTimePipe,
    AppDatePipe,
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

  paymentsPageItems: Payment[] = [];
  paymentsPageTotalCount = 0;
  paymentsPageIndex = 0;
  paymentsPageSize = 10;
  paymentsSortBy?: string;
  paymentsSortDir?: 'asc' | 'desc';

  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;
  paymentMethodLabel = PAYMENT_METHOD_LABEL;
  auditActionLabel = AUDIT_ACTION_LABEL;

  paymentColumns = ['date', 'amount', 'method', 'balance', 'reference', 'createdAt', 'notes', 'actions'];
  paymentFooterColumns = ['footerLabel', 'footerAmount', 'footerMethod', 'footerReference', 'footerNotes', 'footerActions'];
  extensionColumns = ['date', 'days', 'charges', 'balance', 'remarks', 'actions'];
  extensionFooterColumns = ['footerLabel', 'footerDays', 'footerCharges', 'footerRemarks', 'footerActions'];
  auditLogColumns = ['occurredAt', 'action', 'description', 'performedBy'];

  private readonly route = inject(ActivatedRoute);
  private loanId!: string;

  constructor(
    private readonly router: Router,
    private readonly dialog: MatDialog,
    private readonly getLoanDetail: GetLoanDetailUseCase,
    private readonly getLoanPaymentsPage: GetLoanPaymentsPageUseCase,
    private readonly changeClassification: ChangeLoanClassificationUseCase,
    private readonly deletePayment: DeletePaymentUseCase,
    private readonly deleteExtension: DeleteExtensionUseCase,
    private readonly downloadLoanSoa: DownloadLoanSoaUseCase,
    private readonly confirmDialog: ConfirmDialogService,
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

    this.loadPaymentsPage();
  }

  private loadPaymentsPage(): void {
    this.getLoanPaymentsPage
      .execute(this.loanId, this.paymentsPageIndex, this.paymentsPageSize, this.paymentsSortBy, this.paymentsSortDir)
      .subscribe((result) => {
        this.paymentsPageItems = result.items;
        this.paymentsPageTotalCount = result.totalCount;
      });
  }

  onPaymentsPage(event: PageEvent): void {
    this.paymentsPageIndex = event.pageIndex;
    this.paymentsPageSize = event.pageSize;
    this.loadPaymentsPage();
  }

  onPaymentsSort(sort: Sort): void {
    this.paymentsSortBy = sort.direction ? sort.active : undefined;
    this.paymentsSortDir = sort.direction || undefined;
    this.paymentsPageIndex = 0;
    this.loadPaymentsPage();
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

  /** Newest-first, capped at 5 — the Overview tab's "Payment History" mini widget (the full, unlimited list lives on the Payments tab). */
  get recentPayments(): Payment[] {
    return [...this.payments].sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime()).slice(0, 5);
  }

  /** Share of TotalAmountDue collected so far, capped at 100 — drives the Financial Summary donut. */
  get paidPercent(): number {
    if (!this.loan || this.loan.totalAmountDue <= 0) return 0;
    return Math.min(100, Math.round((this.loan.totalPaid / this.loan.totalAmountDue) * 100));
  }

  /**
   * Financial Summary's status message — priority order: a settled/removed
   * loan's message always wins regardless of classification (WrittenOff >
   * Paid), then the lender's own Bad Loan judgment, then the system-derived
   * Overdue/Due Soon/Active states. Exact wording per spec.
   */
  get statusMessage(): StatusMessage {
    const loan = this.loan!;
    const balance = fmtMoney(loan.balance);

    if (loan.status === 'writtenoff') {
      return { tone: 'neutral', icon: 'block', text: `This loan has been written off. The remaining ${balance} is excluded from collectible receivables.` };
    }
    if (loan.status === 'paid') {
      return { tone: 'good', icon: 'check_circle', text: 'This loan is fully paid. All amounts have been settled.' };
    }
    if (loan.classification === 'badloan') {
      return { tone: 'danger', icon: 'warning', text: `This loan is classified as a bad loan. ${balance} remains outstanding and is considered at risk.` };
    }
    if (loan.status === 'overdue') {
      return { tone: 'danger', icon: 'error', text: `This loan is overdue. ${balance} remains outstanding and payment is past due.` };
    }

    // Server-computed (business-local "today" vs. dueDate) rather than
    // recomputed here from the browser's own clock — see loan.entity.ts's
    // daysUntilDue doc comment for why that would risk disagreeing with the
    // backend's own Overdue/Active status decision.
    const daysUntilDue = loan.daysUntilDue ?? Math.ceil((new Date(loan.dueDate).getTime() - Date.now()) / 86_400_000);
    if (daysUntilDue >= 0 && daysUntilDue <= DUE_SOON_DAYS) {
      // dueDate is a plain business date with no time-of-day/timezone
      // meaning — format in UTC so no browser timezone can shift the day,
      // same reasoning as the appDate pipe used in templates.
      const dueDateLabel = new Date(loan.dueDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' });
      return { tone: 'warn', icon: 'schedule', text: `This loan is due soon. ${balance} remains outstanding and is due on ${dueDateLabel}.` };
    }

    return { tone: 'neutral', icon: 'info', text: `This loan is currently active. ${balance} remains outstanding.` };
  }

  /** Newest-first event feed for the Overview tab's Timeline card — loan created, every payment, and every audit-logged edit/classification-change/write-off. */
  get timelineEvents(): TimelineEvent[] {
    if (!this.loan) return [];

    const paymentEvents: TimelineEvent[] = this.payments.map((p) => ({
      icon: 'attach_money',
      tone: 'good',
      title: 'Payment made',
      subtitle: `Payment of ${fmtMoney(p.amountPaid)}`,
      date: p.paymentDate,
    }));

    const createdEvent: TimelineEvent = {
      icon: 'receipt_long',
      tone: 'neutral',
      title: 'Loan created',
      subtitle: `Loan ${this.loan.loanNumber} created`,
      date: this.loan.createdAt,
    };

    const auditEvents: TimelineEvent[] = this.auditLog.map((a) => ({
      icon: a.action === 'written_off' ? 'block' : a.action === 'classification_changed' ? 'flag' : 'edit',
      tone: a.action === 'written_off' ? 'info' : 'neutral',
      title: this.getAuditActionLabel(a.action),
      subtitle: a.description,
      date: a.occurredAt,
    }));

    return [...paymentEvents, createdEvent, ...auditEvents]
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
      .slice(0, 6);
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

  get totalExtensionCharges(): number {
    return this.extensions.reduce((sum, e) => sum + e.additionalChargesAmount, 0);
  }

  back(): void {
    this.router.navigate(['/loans']);
  }

  openCustomerProfile(): void {
    if (!this.loan) return;
    this.router.navigate(['/customers', this.loan.customerId]);
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
      .open(AddPaymentDialogComponent, { width: '420px', maxWidth: '95vw', data: { loanId: this.loan.loanId, balance: this.loan.balance, dailyPayment: this.loan.dailyPayment } })
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

  async confirmDeletePayment(payment: Payment): Promise<void> {
    if (!this.loan) return;
    const ok = await this.confirmDialog.confirm({
      title: 'Delete payment?',
      message: `Delete this payment of ${fmtMoney(payment.amountPaid)} recorded on ${payment.paymentDate}? This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
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

  async confirmDeleteExtension(extension: LoanExtension): Promise<void> {
    if (!this.loan) return;
    const ok = await this.confirmDialog.confirm({
      title: 'Delete extension?',
      message: `Delete this ${extension.extensionDays}-day extension? The due date and charges will revert. This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
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
