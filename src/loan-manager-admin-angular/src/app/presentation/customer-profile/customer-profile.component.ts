import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { Customer } from '../../domain/entities/customer.entity';
import { Loan, LoanClassification, LoanStatus } from '../../domain/entities/loan.entity';
import { PaymentMethod } from '../../domain/entities/payment.entity';
import { GetCustomerDetailUseCase } from '../../application/use-cases/get-customer-detail.use-case';
import { GetCustomerPaymentHistoryUseCase } from '../../application/use-cases/get-customer-payment-history.use-case';
import { GetCustomerPaymentsPageUseCase } from '../../application/use-cases/get-customer-payments-page.use-case';
import { CustomerPayment } from '../../domain/entities/customer-payment.entity';
import { DownloadLoanSoaUseCase } from '../../application/use-cases/download-loan-soa.use-case';
import { DocumentManagerComponent } from '../document-manager/document-manager.component';
import { PromiseListComponent } from '../promise-list/promise-list.component';
import { EditCustomerDialogComponent } from '../edit-customer-dialog/edit-customer-dialog.component';
import { AddLoanDialogComponent } from '../add-loan-dialog/add-loan-dialog.component';
import { AddPaymentDialogComponent } from '../add-payment-dialog/add-payment-dialog.component';
import { AuthService } from '../../application/auth/auth.service';
import { AppDateTimePipe } from '../shared/app-date-time.pipe';
import { AppDatePipe } from '../shared/app-date.pipe';

function fmtMoney(n: number): string {
  return `₱${n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

const PAYMENT_METHOD_LABEL: Record<PaymentMethod, string> = {
  cash: 'Cash',
  gcash: 'GCash',
  bank_transfer: 'Bank transfer',
  other: 'Other',
};

const CLASSIFICATION_LABEL: Record<LoanClassification, string> = {
  normal: 'Normal',
  watchlist: 'Watch List',
  badloan: 'Bad Loan',
};

interface FinancialKpi {
  label: string;
  value: string;
  caption: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn';
  /** Active Loans' "View loans →" caption switches to the Loans tab instead of just being static text. */
  captionClickable?: boolean;
}

interface ActivityItem {
  icon: string;
  tone: 'good' | 'neutral' | 'info';
  title: string;
  subtitle: string;
  /**
   * A mix of business dates (payment.paymentDate) and true timestamps
   * (loan.createdAt, customer.createdAt) — rendered with `appDateTime` in
   * the template, which is correct for the timestamp entries and correct
   * for the date-only entries too as long as the configured Business Time
   * Zone stays east of UTC (true for the default, Asia/Manila); a business
   * timezone west of UTC could show a payment-date entry a day early here.
   */
  date: string;
}

const TAB_LOANS = 0;
const TAB_PAYMENT_HISTORY = 1;
const TAB_DOCUMENTS = 2;
const TAB_NOTES = 3;

function relativeFromNow(dateStr: string): string {
  const days = Math.floor((Date.now() - new Date(dateStr).getTime()) / 86_400_000);
  if (days <= 0) return 'Today';
  if (days === 1) return 'Yesterday';
  if (days < 30) return `${days} days ago`;
  const months = Math.floor(days / 30);
  if (months < 12) return `${months} month${months > 1 ? 's' : ''} ago`;
  const years = Math.floor(months / 12);
  return `${years} year${years > 1 ? 's' : ''} ago`;
}

/**
 * Presentation layer — SRS wireframe 4 (Customer Profile), redesigned per
 * Customer-detail-page.png: header identity card with contact info, a
 * six-card financial summary row, Loans/Documents/Notes/Payment History
 * tabs, and a Customer Information / Recent Activity / Quick Actions
 * footer. Editing the profile and creating a loan both moved from the old
 * inline form into modal dialogs (EditCustomerDialogComponent /
 * AddLoanDialogComponent), matching how every other aggregate in this app
 * is edited/created.
 */
@Component({
  selector: 'lm-customer-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatTabsModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    DocumentManagerComponent,
    PromiseListComponent,
    AppDateTimePipe,
    AppDatePipe,
  ],
  templateUrl: './customer-profile.component.html',
  styleUrls: ['./customer-profile.component.scss'],
})
export class CustomerProfileComponent implements OnInit {
  customer: Customer | null = null;
  loans: Loan[] = [];
  payments: CustomerPayment[] = [];
  paymentHistoryItems: CustomerPayment[] = [];
  paymentHistoryTotalCount = 0;
  paymentHistoryPageIndex = 0;
  paymentHistoryPageSize = 10;
  financialKpis: FinancialKpi[] = [];
  recentActivity: ActivityItem[] = [];
  loading = true;
  generatingSoa = false;

  selectedTabIndex = TAB_LOANS;

  displayedColumns = [
    'id', 'loanDate', 'principal', 'interestPercent', 'interestCharges', 'dueDate',
    'totalPaid', 'balance', 'terms', 'status', 'classification', 'actions',
  ];
  paymentColumns = ['date', 'loan', 'amount', 'method', 'reference', 'createdAt', 'notes'];
  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;
  methodLabel = PAYMENT_METHOD_LABEL;

  searchTerm = '';
  filteredLoans: Loan[] = [];
  private loanSortActive?: string;
  private loanSortDirection?: 'asc' | 'desc';

  paymentSortBy?: string;
  paymentSortDir?: 'asc' | 'desc';

  @ViewChild(DocumentManagerComponent) documentManager?: DocumentManagerComponent;

  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private customerId!: string;

  loanFilters = this.fb.group({
    status: [null as LoanStatus | null],
    classification: [null as LoanClassification | null],
  });

  constructor(
    private readonly getCustomerDetail: GetCustomerDetailUseCase,
    private readonly getCustomerPaymentHistory: GetCustomerPaymentHistoryUseCase,
    private readonly getCustomerPaymentsPage: GetCustomerPaymentsPageUseCase,
    private readonly downloadLoanSoa: DownloadLoanSoaUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.getCustomerDetail.execute(this.customerId).subscribe(({ customer, loans, receivables }) => {
      this.customer = customer ?? null;
      this.loans = loans;
      this.applyLoanFilters();
      this.loading = false;

      const totalPaid = loans.reduce((sum, l) => sum + l.totalPaid, 0);

      this.financialKpis = [
        { label: 'Active Loans', value: `${receivables.activeLoansCount}`, caption: 'View loans →', icon: 'receipt_long', tone: 'neutral', captionClickable: true },
        { label: 'Gross Receivables', value: fmtMoney(receivables.grossReceivables), caption: 'Total outstanding', icon: 'account_balance_wallet', tone: 'neutral' },
        { label: 'Collectible Receivables', value: fmtMoney(receivables.collectibleReceivables), caption: 'Excludes bad loans', icon: 'savings', tone: 'good' },
        { label: 'Bad Loan Receivables', value: fmtMoney(receivables.badLoanReceivables), caption: 'High risk amount', icon: 'warning_amber', tone: receivables.badLoanReceivables > 0 ? 'warn' : 'neutral' },
        { label: 'Total Paid', value: fmtMoney(totalPaid), caption: 'All time payments', icon: 'account_balance', tone: 'good' },
        { label: 'Last Payment', value: '—', caption: 'No payments yet', icon: 'event', tone: 'neutral' },
      ];

      this.buildRecentActivity();
    });

    this.getCustomerPaymentHistory.execute(this.customerId).subscribe((payments) => {
      this.payments = payments;

      if (payments.length > 0) {
        const last = payments[0]; // newest-first
        const lastPaymentKpi = this.financialKpis.find((k) => k.label === 'Last Payment');
        if (lastPaymentKpi) {
          lastPaymentKpi.value = fmtMoney(last.amountPaid);
          lastPaymentKpi.caption = `${new Date(last.paymentDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })} · ${relativeFromNow(last.paymentDate)}`;
        }
      }

      this.buildRecentActivity();
    });

    this.loadPaymentHistoryPage();
  }

  private loadPaymentHistoryPage(): void {
    this.getCustomerPaymentsPage
      .execute(this.customerId, this.paymentHistoryPageIndex, this.paymentHistoryPageSize, this.paymentSortBy, this.paymentSortDir)
      .subscribe((result) => {
        this.paymentHistoryItems = result.items;
        this.paymentHistoryTotalCount = result.totalCount;
      });
  }

  onPaymentHistoryPage(event: PageEvent): void {
    this.paymentHistoryPageIndex = event.pageIndex;
    this.paymentHistoryPageSize = event.pageSize;
    this.loadPaymentHistoryPage();
  }

  onPaymentHistorySort(sort: Sort): void {
    this.paymentSortBy = sort.direction ? sort.active : undefined;
    this.paymentSortDir = sort.direction || undefined;
    this.paymentHistoryPageIndex = 0;
    this.loadPaymentHistoryPage();
  }

  private buildRecentActivity(): void {
    if (!this.customer) return;

    const paymentEvents: ActivityItem[] = this.payments.map((p) => ({
      icon: 'attach_money',
      tone: 'good',
      title: 'Payment received',
      subtitle: `Loan ${p.loanNumber} - ${fmtMoney(p.amountPaid)}`,
      date: p.paymentDate,
    }));

    const loanEvents: ActivityItem[] = this.loans.map((l) => ({
      icon: 'receipt_long',
      tone: 'neutral',
      title: 'Loan created',
      subtitle: `${l.loanNumber} - ${fmtMoney(l.principalAmount)}`,
      date: l.createdAt,
    }));

    const customerEvent: ActivityItem = {
      icon: 'person_add',
      tone: 'info',
      title: 'Customer created',
      subtitle: this.customer.customerCode,
      date: this.customer.createdAt,
    };

    this.recentActivity = [...paymentEvents, ...loanEvents, customerEvent]
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
      .slice(0, 5);
  }

  get initials(): string {
    if (!this.customer) return '';
    return this.customer.fullName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('');
  }

  get primaryLoanForSoa(): Loan | undefined {
    return [...this.loans].sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())[0];
  }

  onSearchChange(term: string): void {
    this.searchTerm = term;
    this.applyLoanFilters();
  }

  applyLoanFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();
    const { status, classification } = this.loanFilters.value;
    this.filteredLoans = this.loans.filter((loan) => {
      if (term && !loan.loanNumber.toLowerCase().includes(term)) return false;
      if (status && loan.status !== status) return false;
      if (classification && loan.classification !== classification) return false;
      return true;
    });
    this.resortFilteredLoans();
  }

  onLoansSort(sort: Sort): void {
    this.loanSortActive = sort.direction ? sort.active : undefined;
    this.loanSortDirection = sort.direction || undefined;
    this.resortFilteredLoans();
  }

  private resortFilteredLoans(): void {
    if (!this.loanSortActive || !this.loanSortDirection) return;
    const key = this.loanSortActive;
    const dir = this.loanSortDirection === 'asc' ? 1 : -1;
    this.filteredLoans = [...this.filteredLoans].sort((a, b) => {
      const va = this.loanSortValue(a, key);
      const vb = this.loanSortValue(b, key);
      if (va < vb) return -1 * dir;
      if (va > vb) return 1 * dir;
      return 0;
    });
  }

  private loanSortValue(loan: Loan, key: string): string | number {
    switch (key) {
      case 'loanNumber': return loan.loanNumber;
      case 'loanDate': return loan.startDate;
      case 'principal': return loan.principalAmount;
      case 'interestPercent': return loan.interestRate;
      case 'interestCharges': return loan.totalInterest + loan.totalExtensionCharges;
      case 'dueDate': return loan.dueDate;
      case 'totalPaid': return loan.totalPaid;
      case 'balance': return loan.balance;
      case 'terms': return loan.paymentTermsMonths;
      case 'status': return loan.status;
      case 'classification': return loan.classification;
      default: return '';
    }
  }

  get filteredPrincipalTotal(): number {
    return this.filteredLoans.reduce((sum, l) => sum + l.principalAmount, 0);
  }

  get filteredInterestAndChargesTotal(): number {
    return this.filteredLoans.reduce((sum, l) => sum + l.totalInterest + l.totalExtensionCharges, 0);
  }

  get filteredTotalPaidTotal(): number {
    return this.filteredLoans.reduce((sum, l) => sum + l.totalPaid, 0);
  }

  get filteredBalanceTotal(): number {
    return this.filteredLoans.reduce((sum, l) => sum + l.balance, 0);
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  getClassificationLabel(classification: LoanClassification): string {
    return this.classificationLabel[classification];
  }

  getMethodLabel(method: PaymentMethod): string {
    return this.methodLabel[method];
  }

  focusLoansTab(): void {
    this.selectedTabIndex = TAB_LOANS;
  }

  openLoanDetails(loan: Loan): void {
    this.router.navigate(['/loans', loan.loanId]);
  }

  openLoanFromPayment(payment: CustomerPayment): void {
    this.router.navigate(['/loans', payment.loanId]);
  }

  openAddPayment(loan: Loan): void {
    this.dialog
      .open(AddPaymentDialogComponent, { width: '420px', maxWidth: '95vw', data: { loanId: loan.loanId, balance: loan.balance, dailyPayment: loan.dailyPayment } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.recorded) this.load();
      });
  }

  viewAllLoans(): void {
    this.router.navigate(['/loans']);
  }

  openEditCustomer(): void {
    if (!this.customer) return;
    this.dialog
      .open(EditCustomerDialogComponent, { width: '480px', maxWidth: '95vw', data: { customer: this.customer } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.updated) this.customer = result.customer;
      });
  }

  openAddLoan(): void {
    if (!this.customer) return;
    this.dialog
      .open(AddLoanDialogComponent, {
        width: '480px',
        maxWidth: '95vw',
        data: { customerId: this.customer.customerId, customerName: this.customer.fullName },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.load();
      });
  }

  quickUploadDocument(): void {
    this.selectedTabIndex = TAB_DOCUMENTS;
    setTimeout(() => this.documentManager?.openFilePicker());
  }

  quickAddNote(): void {
    this.selectedTabIndex = TAB_NOTES;
  }

  viewStatementOfAccount(): void {
    const loan = this.primaryLoanForSoa;
    if (!loan || this.generatingSoa) return;
    this.generatingSoa = true;
    this.downloadLoanSoa.execute(loan.loanId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `SOA-${loan.loanNumber}.pdf`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 30_000);
        this.generatingSoa = false;
      },
      error: () => {
        this.generatingSoa = false;
      },
    });
  }

  back(): void {
    this.router.navigate(['/customers']);
  }
}
