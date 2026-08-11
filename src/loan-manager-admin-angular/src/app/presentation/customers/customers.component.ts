import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';

import { Customer, CustomerStatus } from '../../domain/entities/customer.entity';
import { CustomerListItem } from '../../domain/repositories/loan.repository';
import { GetCustomersPageUseCase } from '../../application/use-cases/get-customers-page.use-case';
import { AddCustomerDialogComponent } from '../add-customer-dialog/add-customer-dialog.component';
import { AuthService } from '../../application/auth/auth.service';

const STATUS_LABEL: Record<CustomerStatus, string> = {
  active: 'Active',
  inactive: 'Inactive',
};

/** Maps mat-sort-header column ids (template) to the backend's GetCustomersPage sortBy values. */
const SORT_KEY: Record<string, string> = {
  name: 'fullName',
  borrowerType: 'borrowerType',
};

/**
 * Presentation layer — SRS wireframe 3 (Customers Page): searchable list of
 * customer profiles with an "Add Customer" button, linking through to each
 * customer's profile (SRS wireframe 4). Paging, sorting, and search are all
 * server-side — dataSource holds only the current page's rows, so its
 * `.sort`/`.paginator` are intentionally never wired to MatSort/MatPaginator.
 */
@Component({
  selector: 'lm-customers',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
  ],
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.scss'],
})
export class CustomersComponent implements OnInit, OnDestroy {
  displayedColumns = ['customerCode', 'name', 'contact', 'borrowerType', 'status', 'loanCount'];
  dataSource = new MatTableDataSource<CustomerListItem>([]);
  statusLabel = STATUS_LABEL;

  pageIndex = 0;
  pageSize = 10;
  totalCount = 0;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  private searchTerm = '';

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly search$ = new Subject<string>();
  private readonly searchSubscription: Subscription;

  constructor(
    private readonly getCustomersPage: GetCustomersPageUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    readonly authService: AuthService,
  ) {
    this.searchSubscription = this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.pageIndex = 0;
      this.load();
    });
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private load(): void {
    this.getCustomersPage.execute(this.pageIndex, this.pageSize, this.searchTerm, this.sortBy, this.sortDir).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });
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

  applyFilter(value: string): void {
    this.search$.next(value.trim().toLowerCase());
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
}
