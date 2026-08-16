import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';

import { Customer, CustomerStatus } from '../../domain/entities/customer.entity';
import { CustomerTotals } from '../../domain/entities/customer-totals.entity';
import { CustomerListItem } from '../../domain/repositories/loan.repository';
import { GetCustomersPageUseCase } from '../../application/use-cases/get-customers-page.use-case';
import { GetCustomersTotalsUseCase } from '../../application/use-cases/get-customers-totals.use-case';
import { ExportCustomersUseCase } from '../../application/use-cases/export-customers.use-case';
import { AddCustomerDialogComponent } from '../add-customer-dialog/add-customer-dialog.component';
import { AuthService } from '../../application/auth/auth.service';
import { todayLocalDateString } from '../shared/date-utils';

const STATUS_LABEL: Record<CustomerStatus, string> = {
  active: 'Active',
  inactive: 'Inactive',
};

/** Maps mat-sort-header column ids (template) to the backend's GetCustomersPage sortBy values. */
const SORT_KEY: Record<string, string> = {
  name: 'fullName',
  borrowerType: 'borrowerType',
};

interface CustomerKpi {
  label: string;
  value: string;
  sub: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn' | 'info' | 'danger';
}

function fmtMoney(n: number): string {
  return `₱${n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * Presentation layer — SRS wireframe 3 (Customers Page): searchable list of
 * customer profiles with an "Add Customer" button, linking through to each
 * customer's profile (SRS wireframe 4). Layout follows the
 * "customers-list-modern.html" reference (filter card with search + status
 * dropdown, a 5-metric KPI strip, then the table card). Paging, sorting,
 * and search are all server-side — dataSource holds only the current
 * page's rows, so its `.sort`/`.paginator` are intentionally never wired to
 * MatSort/MatPaginator. The KPI strip (spec-equivalent "Customer Grid
 * Footer Totals") is fetched separately via GetCustomersTotalsUseCase since
 * it summarizes the whole filtered set, not just the visible page.
 */
@Component({
  selector: 'lm-customers',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatSelectModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
  ],
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.scss'],
})
export class CustomersComponent implements OnInit, OnDestroy {
  displayedColumns = ['customerCode', 'name', 'contact', 'borrowerType', 'status', 'loanCount', 'outstandingBalance', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<CustomerListItem>([]);
  statusLabel = STATUS_LABEL;
  totals: CustomerTotals | null = null;
  kpis: CustomerKpi[] = [];

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
    status: [null as CustomerStatus | null],
  });

  constructor(
    private readonly getCustomersPage: GetCustomersPageUseCase,
    private readonly getCustomersTotals: GetCustomersTotalsUseCase,
    private readonly exportCustomers: ExportCustomersUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
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
    this.load();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private currentStatus(): CustomerStatus | undefined {
    return this.filters.getRawValue().status ?? undefined;
  }

  private load(): void {
    const status = this.currentStatus();

    this.getCustomersPage.execute(this.pageIndex, this.pageSize, this.searchTerm, this.sortBy, this.sortDir, status).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });

    this.getCustomersTotals.execute(this.searchTerm, status).subscribe((totals) => {
      this.totals = totals;
      this.kpis = this.buildKpis(totals);
    });
  }

  private buildKpis(totals: CustomerTotals): CustomerKpi[] {
    const pct = (count: number) => (totals.totalCustomersCount > 0 ? `${((count / totals.totalCustomersCount) * 100).toFixed(1)}% of total` : '—');
    return [
      { label: 'Total Customers', value: `${totals.totalCustomersCount}`, sub: 'All time', icon: 'groups', tone: 'neutral' },
      { label: 'Active Customers', value: `${totals.activeCustomersCount}`, sub: pct(totals.activeCustomersCount), icon: 'check_circle', tone: 'good' },
      { label: 'Inactive Customers', value: `${totals.inactiveCustomersCount}`, sub: pct(totals.inactiveCustomersCount), icon: 'schedule', tone: 'warn' },
      { label: 'Total Loans', value: `${totals.totalLoansCount}`, sub: 'Across all customers', icon: 'receipt_long', tone: 'info' },
      { label: 'Total Outstanding Balance', value: fmtMoney(totals.totalOutstandingBalance), sub: 'Across all customers', icon: 'warning_amber', tone: 'danger' },
    ];
  }

  applyFilters(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({ status: null });
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

  getStatusLabel(status: CustomerStatus): string {
    return this.statusLabel[status];
  }

  openCustomerProfile(customer: Customer): void {
    this.router.navigate(['/customers', customer.customerId]);
  }

  openAddCustomer(): void {
    this.dialog
      .open(AddCustomerDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.load();
      });
  }

  export(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.exportCustomers.execute(this.searchTerm, this.currentStatus()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `customers_export_${todayLocalDateString()}.xlsx`;
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
