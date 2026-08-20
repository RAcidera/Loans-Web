import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';

import { Loan, LoanClassification, LoanPageFilters, LoanStatus } from '../../domain/entities/loan.entity';
import { LoanTotals } from '../../domain/entities/loan-totals.entity';
import { GetLoansPageUseCase } from '../../application/use-cases/get-loans-page.use-case';
import { GetLoansTotalsUseCase } from '../../application/use-cases/get-loans-totals.use-case';
import { ExportLoansUseCase } from '../../application/use-cases/export-loans.use-case';
import { AddLoanDialogComponent } from '../add-loan-dialog/add-loan-dialog.component';
import { AuthService } from '../../application/auth/auth.service';
import { toLocalDateString, todayLocalDateString } from '../shared/date-utils';
import { AppDatePipe } from '../shared/app-date.pipe';

/** Maps mat-sort-header column ids (template) to the backend's GetLoansPage sortBy values. */
const SORT_KEY: Record<string, string> = {
  loanNumber: 'loanNumber',
  customer: 'customer',
  loanDate: 'loanDate',
  principal: 'principalAmount',
  dueDate: 'dueDate',
  balance: 'balance',
};

interface LoanKpi {
  label: string;
  value: string;
  sub: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn' | 'info' | 'danger';
}

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

function toDateString(date: Date | null): string | undefined {
  return date ? toLocalDateString(date) : undefined;
}

function fmtMoney(n: number): string {
  return `₱${n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * Presentation layer — SRS wireframe 3.2/3.5: the full outstanding-loans
 * list, as opposed to the dashboard's abbreviated KPI-plus-table view.
 * Layout follows the "loans-list-modern.html" reference (filter card with
 * search + dropdowns + date ranges, a 5-metric KPI strip, then the table
 * card). Paging, sorting, and search are all server-side — dataSource holds
 * only the current page's rows, so its `.sort`/`.paginator` are intentionally
 * never wired to MatSort/MatPaginator. Sorting by "customer" and "loanDate"
 * is backed by LoanRepository.GetPageAsync joining/ordering server-side
 * (Loan and Customer are separate aggregates with no EF navigation property,
 * but still share one DbContext — see LoanRepository.OrderByCustomerName).
 * Footer totals and the KPI strip (spec "Loan Grid Footer Totals" plus
 * status counts) are fetched separately via GetLoansTotalsUseCase since they
 * summarize the whole filtered set, not just the visible page.
 */
@Component({
  selector: 'lm-loans',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    AppDatePipe,
  ],
  templateUrl: './loans.component.html',
  styleUrls: ['./loans.component.scss'],
})
export class LoansComponent implements OnInit, OnDestroy {
  displayedColumns = [
    'loanNumber', 'customer', 'loanDate', 'principal', 'interest', 'extensionCharges', 'totalPayments',
    'dueDate', 'balance', 'status', 'classification', 'actions',
  ];
  dataSource = new MatTableDataSource<Loan>([]);
  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;
  totals: LoanTotals | null = null;
  kpis: LoanKpi[] = [];

  pageIndex = 0;
  pageSize = 10;
  totalCount = 0;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  exporting = false;
  private searchTerm = '';

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly search$ = new Subject<string>();
  private readonly searchSubscription: Subscription;

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    statuses: [[] as LoanStatus[]],
    classifications: [[] as LoanClassification[]],
    loanDateFrom: [null as Date | null],
    loanDateTo: [null as Date | null],
    dueDateFrom: [null as Date | null],
    dueDateTo: [null as Date | null],
  });

  constructor(
    private readonly getLoansPage: GetLoansPageUseCase,
    private readonly getLoansTotals: GetLoansTotalsUseCase,
    private readonly exportLoans: ExportLoansUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    readonly authService: AuthService,
  ) {
    this.searchSubscription = this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.pageIndex = 0;
      if (this.paginator) this.paginator.pageIndex = 0;
      this.load();
    });
  }

  ngOnInit(): void {
    // Supports deep-linking from the Dashboard KPI cards, e.g. /loans?status=overdue —
    // pre-applies the matching filter before the first load rather than requiring a second click.
    const status = this.route.snapshot.queryParamMap.get('status') as LoanStatus | null;
    const classification = this.route.snapshot.queryParamMap.get('classification') as LoanClassification | null;
    if (status) this.filters.patchValue({ statuses: [status] });
    if (classification) this.filters.patchValue({ classifications: [classification] });
    this.load();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private currentFilters(): LoanPageFilters {
    const raw = this.filters.getRawValue();
    return {
      statuses: raw.statuses?.length ? raw.statuses : undefined,
      classifications: raw.classifications?.length ? raw.classifications : undefined,
      loanDateFrom: toDateString(raw.loanDateFrom),
      loanDateTo: toDateString(raw.loanDateTo),
      dueDateFrom: toDateString(raw.dueDateFrom),
      dueDateTo: toDateString(raw.dueDateTo),
    };
  }

  private load(): void {
    const filters = this.currentFilters();

    this.getLoansPage.execute(this.pageIndex, this.pageSize, this.searchTerm, this.sortBy, this.sortDir, filters).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });

    this.getLoansTotals.execute(this.searchTerm, filters).subscribe((totals) => {
      this.totals = totals;
      this.kpis = this.buildKpis(totals);
    });
  }

  private buildKpis(totals: LoanTotals): LoanKpi[] {
    const pct = (count: number) => (totals.totalLoansCount > 0 ? `${((count / totals.totalLoansCount) * 100).toFixed(1)}% of total` : '—');
    return [
      { label: 'Total Loans', value: `${totals.totalLoansCount}`, sub: 'All time', icon: 'receipt_long', tone: 'neutral' },
      { label: 'Active Loans', value: `${totals.activeLoansCount}`, sub: pct(totals.activeLoansCount), icon: 'check_circle', tone: 'good' },
      { label: 'Overdue Loans', value: `${totals.overdueLoansCount}`, sub: pct(totals.overdueLoansCount), icon: 'schedule', tone: 'warn' },
      { label: 'Paid Loans', value: `${totals.paidLoansCount}`, sub: pct(totals.paidLoansCount), icon: 'task_alt', tone: 'info' },
      { label: 'Total Outstanding Balance', value: fmtMoney(totals.totalOutstandingBalance), sub: 'Across all loans', icon: 'warning_amber', tone: 'danger' },
    ];
  }

  applyFilters(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({
      statuses: [], classifications: [], loanDateFrom: null, loanDateTo: null, dueDateFrom: null, dueDateTo: null,
    });
    this.applyFilters();
  }

  applySearch(value: string): void {
    this.search$.next(value.trim());
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.load();
  }

  onSort(sort: Sort): void {
    this.sortBy = sort.direction ? SORT_KEY[sort.active] : undefined;
    this.sortDir = sort.direction || undefined;
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.load();
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  getClassificationLabel(classification: LoanClassification): string {
    return this.classificationLabel[classification];
  }

  openLoanDetails(loan: Loan): void {
    this.router.navigate(['/loans', loan.loanId]);
  }

  openCustomerProfile(loan: Loan): void {
    this.router.navigate(['/customers', loan.customerId]);
  }

  openAddLoan(): void {
    this.dialog
      .open(AddLoanDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.load();
      });
  }

  export(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.exportLoans.execute(this.searchTerm, this.currentFilters()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `loans_export_${todayLocalDateString()}.xlsx`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 30_000);
        this.exporting = false;
      },
      error: () => {
        this.exporting = false;
      },
    });
  }
}
