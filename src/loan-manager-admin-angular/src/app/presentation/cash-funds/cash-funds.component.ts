import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';

import { CashLedgerEntry, CashLedgerPageFilters, CashLedgerTotals, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { GetCashSummaryUseCase } from '../../application/use-cases/get-cash-summary.use-case';
import { GetCashLedgerPageUseCase } from '../../application/use-cases/get-cash-ledger-page.use-case';
import { GetCashLedgerTotalsUseCase } from '../../application/use-cases/get-cash-ledger-totals.use-case';
import { DeleteCashTransactionUseCase } from '../../application/use-cases/delete-cash-transaction.use-case';
import { ExportCashLedgerUseCase } from '../../application/use-cases/export-cash-ledger.use-case';
import { AddCashTransactionDialogComponent } from '../add-cash-transaction-dialog/add-cash-transaction-dialog.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { AuthService } from '../../application/auth/auth.service';
import { toLocalDateString, todayLocalDateString } from '../shared/date-utils';
import { AppDatePipe } from '../shared/app-date.pipe';

const TYPE_LABEL: Record<CashTransactionType, string> = {
  loan_release: 'Loan Release',
  payment_received: 'Payment Received',
  owner_deposit: 'Cash Deposit',
  owner_withdrawal: 'Cash Withdrawal',
  expense: 'Expense',
  adjustment: 'Adjustment',
};

function toDateString(date: Date | null): string | undefined {
  return date ? toLocalDateString(date) : undefined;
}

function fmtMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * Presentation layer — the Cash Ledger redesign: one compact Cash on Hand
 * summary card (This Month's Cash In/Cash Out/Net Change as secondary
 * context underneath, not separate KPI cards), a server-side-paged
 * Cash Transactions grid with Cash In/Cash Out split into their own
 * columns plus a Running Balance column, and manual-vs-automatic row
 * actions (loan_release/payment_received rows show a lock icon instead of
 * the Edit/Delete menu — see CashLedgerEntry.IsAutomatic on the backend).
 * Trend badges use the same plain sign-based rule as the Dashboard:
 * positive → green + up arrow, negative → red + down arrow, regardless of
 * whether "more" is actually good for that particular figure (e.g. more
 * Cash Out this month still shows as a plain "up" trend, not a red one) —
 * consistent with how every other trend badge in this app behaves.
 */
@Component({
  selector: 'lm-cash-funds',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDialogModule,
    AppDatePipe,
  ],
  templateUrl: './cash-funds.component.html',
  styleUrls: ['./cash-funds.component.scss'],
})
export class CashFundsComponent implements OnInit, OnDestroy {
  displayedColumns = ['date', 'transaction', 'reference', 'cashIn', 'cashOut', 'balance', 'remarks', 'actions'];
  /** A second footer row aligned to the same 8 column positions as displayedColumns — see the template comment above its <ng-container>s. */
  netRowColumns = ['netLabel', 'netBlank1', 'netBlank2', 'netBlank3', 'netValue', 'netBlank4', 'netBlank5', 'netBlank6'];
  dataSource = new MatTableDataSource<CashLedgerEntry>([]);
  typeLabel = TYPE_LABEL;

  summary: CashSummary | null = null;
  totals: CashLedgerTotals | null = null;

  pageIndex = 0;
  pageSize = 15;
  totalCount = 0;
  exporting = false;
  private searchTerm = '';

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly search$ = new Subject<string>();
  private readonly searchSubscription: Subscription;

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    transactionType: [null as CashTransactionType | null],
    dateFrom: [null as Date | null],
    dateTo: [null as Date | null],
  });

  constructor(
    private readonly getCashSummary: GetCashSummaryUseCase,
    private readonly getCashLedgerPage: GetCashLedgerPageUseCase,
    private readonly getCashLedgerTotals: GetCashLedgerTotalsUseCase,
    private readonly deleteCashTransaction: DeleteCashTransactionUseCase,
    private readonly exportCashLedger: ExportCashLedgerUseCase,
    private readonly dialog: MatDialog,
    private readonly confirmDialog: ConfirmDialogService,
    readonly authService: AuthService,
  ) {
    this.searchSubscription = this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.pageIndex = 0;
      if (this.paginator) this.paginator.pageIndex = 0;
      this.loadPage();
    });
  }

  ngOnInit(): void {
    this.loadSummary();
    this.loadPage();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private currentFilters(): CashLedgerPageFilters {
    const raw = this.filters.getRawValue();
    return {
      search: this.searchTerm || undefined,
      transactionType: raw.transactionType ?? undefined,
      dateFrom: toDateString(raw.dateFrom),
      dateTo: toDateString(raw.dateTo),
    };
  }

  private loadSummary(): void {
    this.getCashSummary.execute().subscribe((summary) => (this.summary = summary));
  }

  private loadPage(): void {
    const filters = this.currentFilters();

    this.getCashLedgerPage.execute(this.pageIndex, this.pageSize, filters).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });

    this.getCashLedgerTotals.execute(filters).subscribe((totals) => (this.totals = totals));
  }

  applyFilters(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.loadPage();
  }

  clearFilters(): void {
    this.filters.reset({ transactionType: null, dateFrom: null, dateTo: null });
    this.applyFilters();
  }

  applySearch(value: string): void {
    this.search$.next(value.trim());
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadPage();
  }

  getTypeLabel(type: CashTransactionType): string {
    return this.typeLabel[type];
  }

  fmtMoney(n: number): string {
    return fmtMoney(n);
  }

  /** Same plain rule as the Dashboard's trend badges — see this component's doc comment. */
  trendIcon(changePercent: number): string {
    return changePercent > 0 ? 'arrow_upward' : changePercent < 0 ? 'arrow_downward' : 'remove';
  }

  trendClass(changePercent: number): string {
    return changePercent > 0 ? 'good' : changePercent < 0 ? 'bad' : '';
  }

  openAddTransaction(): void {
    this.dialog
      .open(AddCashTransactionDialogComponent, { width: '460px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) {
          this.loadSummary();
          this.loadPage();
        }
      });
  }

  openEditTransaction(entry: CashLedgerEntry): void {
    this.dialog
      .open(AddCashTransactionDialogComponent, { width: '460px', maxWidth: '95vw', data: { entry } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) {
          this.loadSummary();
          this.loadPage();
        }
      });
  }

  async confirmDelete(entry: CashLedgerEntry): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Delete transaction?',
      message: `Delete this ${this.getTypeLabel(entry.transactionType).toLowerCase()} of ${fmtMoney(entry.amount)}? This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
    this.deleteCashTransaction.execute(entry.ledgerId).subscribe(() => {
      this.loadSummary();
      this.loadPage();
    });
  }

  export(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.exportCashLedger.execute(this.currentFilters()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `cash_transactions_export_${todayLocalDateString()}.xlsx`;
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
