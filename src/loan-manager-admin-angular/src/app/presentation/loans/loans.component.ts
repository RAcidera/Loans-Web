import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { Loan, LoanClassification, LoanPageFilters, LoanStatus } from '../../domain/entities/loan.entity';
import { LoanTotals } from '../../domain/entities/loan-totals.entity';
import { GetLoansPageUseCase } from '../../application/use-cases/get-loans-page.use-case';
import { GetLoansTotalsUseCase } from '../../application/use-cases/get-loans-totals.use-case';
import { AddLoanDialogComponent } from '../add-loan-dialog/add-loan-dialog.component';
import { AuthService } from '../../application/auth/auth.service';

/** Maps mat-sort-header column ids (template) to the backend's GetLoansPage sortBy values. */
const SORT_KEY: Record<string, string> = {
  loanNumber: 'loanNumber',
  principal: 'principalAmount',
  dueDate: 'dueDate',
  balance: 'balance',
};

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
  return date ? date.toISOString().slice(0, 10) : undefined;
}

/**
 * Presentation layer — SRS wireframe 3.2/3.5: the full outstanding-loans
 * list, as opposed to the dashboard's abbreviated KPI-plus-table view.
 * Paging and sorting are server-side — dataSource holds only the current
 * page's rows, so its `.sort`/`.paginator` are intentionally never wired to
 * MatSort/MatPaginator (sorting by "customer" is left off mat-sort-header
 * since Loan/Customer are separate aggregates and the backend doesn't
 * support ordering by the joined name in v1). Filtering is via the
 * Status/Classification/date-range/checkbox filter bar only — no free-text
 * search field. Footer totals (spec "Loan Grid Footer Totals") are fetched
 * separately via GetLoansTotalsUseCase since they sum the whole filtered
 * set, not just the visible page.
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
    MatCheckboxModule,
    MatDatepickerModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
  ],
  templateUrl: './loans.component.html',
  styleUrls: ['./loans.component.scss'],
})
export class LoansComponent implements OnInit {
  displayedColumns = [
    'loanNumber', 'customer', 'principal', 'interest', 'extensionCharges', 'totalPayments',
    'dueDate', 'balance', 'status', 'classification', 'actions',
  ];
  dataSource = new MatTableDataSource<Loan>([]);
  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;
  totals: LoanTotals | null = null;

  pageIndex = 0;
  pageSize = 10;
  totalCount = 0;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    status: [null as LoanStatus | null],
    classification: [null as LoanClassification | null],
    loanDateFrom: [null as Date | null],
    loanDateTo: [null as Date | null],
    dueDateFrom: [null as Date | null],
    dueDateTo: [null as Date | null],
    badLoansOnly: [false],
    overdueOnly: [false],
  });

  constructor(
    private readonly getLoansPage: GetLoansPageUseCase,
    private readonly getLoansTotals: GetLoansTotalsUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private currentFilters(): LoanPageFilters {
    const raw = this.filters.getRawValue();
    return {
      status: raw.status ?? undefined,
      classification: raw.classification ?? undefined,
      loanDateFrom: toDateString(raw.loanDateFrom),
      loanDateTo: toDateString(raw.loanDateTo),
      dueDateFrom: toDateString(raw.dueDateFrom),
      dueDateTo: toDateString(raw.dueDateTo),
      badLoansOnly: raw.badLoansOnly ?? false,
      overdueOnly: raw.overdueOnly ?? false,
    };
  }

  private load(): void {
    const filters = this.currentFilters();

    this.getLoansPage.execute(this.pageIndex, this.pageSize, '', this.sortBy, this.sortDir, filters).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });

    this.getLoansTotals.execute('', filters).subscribe((totals) => (this.totals = totals));
  }

  applyFilters(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({
      status: null, classification: null, loanDateFrom: null, loanDateTo: null,
      dueDateFrom: null, dueDateTo: null, badLoansOnly: false, overdueOnly: false,
    });
    this.applyFilters();
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

  openAddLoan(): void {
    this.dialog
      .open(AddLoanDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.load();
      });
  }
}
