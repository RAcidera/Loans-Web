import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';

import { PaymentMethod, PaymentPageFilters, PaymentWithCustomer } from '../../domain/entities/payment.entity';
import { GetPaymentsPageUseCase } from '../../application/use-cases/get-payments-page.use-case';
import { GetPaymentsTotalsUseCase } from '../../application/use-cases/get-payments-totals.use-case';
import { ExportPaymentsUseCase } from '../../application/use-cases/export-payments.use-case';
import { toLocalDateString, todayLocalDateString } from '../shared/date-utils';

/** Maps mat-sort-header column ids (template) to the backend's GetPaymentsPage sortBy values. */
const SORT_KEY: Record<string, string> = {
  loanNumber: 'loan',
  customer: 'customer',
  paymentDate: 'date',
  amountPaid: 'amount',
};

const PAYMENT_METHOD_LABEL: Record<PaymentMethod, string> = {
  cash: 'Cash',
  gcash: 'GCash',
  bank_transfer: 'Bank transfer',
  other: 'Other',
};

function toDateString(date: Date | null): string | undefined {
  return date ? toLocalDateString(date) : undefined;
}

/**
 * Presentation layer — standalone Payments list page: every payment across
 * every loan, one row per payment, filterable by loan #/customer/payment
 * date range. Layout follows the Loans list page's own filter-card +
 * table-card structure so the two read as the same app (label-above-control
 * filter fields, mono/right-aligned money columns, a footer totals row).
 * Paging/sorting/search are all server-side, same reasoning as
 * LoansComponent — dataSource holds only the current page's rows.
 */
@Component({
  selector: 'lm-payments',
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
    MatDatepickerModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './payments.component.html',
  styleUrls: ['./payments.component.scss'],
})
export class PaymentsComponent implements OnInit, OnDestroy {
  displayedColumns = ['loanNumber', 'customer', 'paymentDate', 'amountPaid', 'paymentMethod', 'referenceNumber', 'notes'];
  dataSource = new MatTableDataSource<PaymentWithCustomer>([]);
  methodLabel = PAYMENT_METHOD_LABEL;

  totalAmountPaid = 0;

  pageIndex = 0;
  pageSize = 10;
  totalCount = 0;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  exporting = false;

  private loanSearchTerm = '';
  private customerSearchTerm = '';

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly loanSearch$ = new Subject<string>();
  private readonly customerSearch$ = new Subject<string>();
  private readonly searchSubscriptions: Subscription[] = [];

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    dateFrom: [null as Date | null],
    dateTo: [null as Date | null],
  });

  constructor(
    private readonly getPaymentsPage: GetPaymentsPageUseCase,
    private readonly getPaymentsTotals: GetPaymentsTotalsUseCase,
    private readonly exportPayments: ExportPaymentsUseCase,
    private readonly router: Router,
  ) {
    this.searchSubscriptions.push(
      this.loanSearch$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
        this.loanSearchTerm = term;
        this.resetToFirstPage();
        this.load();
      }),
    );
    this.searchSubscriptions.push(
      this.customerSearch$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
        this.customerSearchTerm = term;
        this.resetToFirstPage();
        this.load();
      }),
    );
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.searchSubscriptions.forEach((s) => s.unsubscribe());
  }

  private currentFilters(): PaymentPageFilters {
    const raw = this.filters.getRawValue();
    return {
      loanSearch: this.loanSearchTerm || undefined,
      customerSearch: this.customerSearchTerm || undefined,
      dateFrom: toDateString(raw.dateFrom),
      dateTo: toDateString(raw.dateTo),
    };
  }

  private resetToFirstPage(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
  }

  private load(): void {
    const filters = this.currentFilters();

    this.getPaymentsPage.execute(this.pageIndex, this.pageSize, this.sortBy, this.sortDir, filters).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });

    this.getPaymentsTotals.execute(filters).subscribe((totals) => {
      this.totalAmountPaid = totals.totalAmountPaid;
    });
  }

  applyFilters(): void {
    this.resetToFirstPage();
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({ dateFrom: null, dateTo: null });
    this.loanSearchTerm = '';
    this.customerSearchTerm = '';
    this.applyFilters();
  }

  applyLoanSearch(value: string): void {
    this.loanSearch$.next(value.trim());
  }

  applyCustomerSearch(value: string): void {
    this.customerSearch$.next(value.trim());
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.load();
  }

  onSort(sort: Sort): void {
    this.sortBy = sort.direction ? SORT_KEY[sort.active] : undefined;
    this.sortDir = sort.direction || undefined;
    this.resetToFirstPage();
    this.load();
  }

  getMethodLabel(method: PaymentMethod): string {
    return this.methodLabel[method];
  }

  openLoanDetails(payment: PaymentWithCustomer): void {
    this.router.navigate(['/loans', payment.loanId]);
  }

  openCustomerProfile(payment: PaymentWithCustomer): void {
    this.router.navigate(['/customers', payment.customerId]);
  }

  export(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.exportPayments.execute(this.currentFilters()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `payments_export_${todayLocalDateString()}.xlsx`;
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
