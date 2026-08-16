import { Component, OnInit, OnDestroy, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged, forkJoin } from 'rxjs';

import { InterestEarnedFilters, InterestEarnedMonthlyPoint, InterestEarnedOverview, InterestEarnedRow, InterestType } from '../../domain/entities/interest-earned-report.entity';
import { LoanClassification, LoanStatus } from '../../domain/entities/loan.entity';
import { GetInterestEarnedPageUseCase } from '../../application/use-cases/get-interest-earned-page.use-case';
import { GetInterestEarnedOverviewUseCase } from '../../application/use-cases/get-interest-earned-overview.use-case';
import { GetInterestEarnedMonthlyChartUseCase } from '../../application/use-cases/get-interest-earned-monthly-chart.use-case';
import { ExportInterestEarnedXlsxUseCase, ExportInterestEarnedPdfUseCase } from '../../application/use-cases/export-interest-earned.use-case';
import { InterestEarnedBreakdownDialogComponent } from '../interest-earned-breakdown-dialog/interest-earned-breakdown-dialog.component';
import { firstOfMonthLocalDateString, todayLocalDateString } from '../shared/date-utils';
import { ChartTooltipDirective } from '../shared/chart-tooltip.directive';

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

function fmtMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

interface BarGroup {
  month: string;
  currentHeight: number;
  previousHeight: number;
  currentTooltip: string;
  previousTooltip: string;
}

/**
 * Presentation layer — the Interest Earned Report (Reports > Interest
 * Earned). All calculation happens server-side (InterestCalculationService);
 * this component only sends filters, displays results/charts, and handles
 * paging/sorting/export triggers, per the report spec's performance
 * requirements. The grid and overview (6 KPI cards + footer totals) are
 * fetched separately since the grid is paged and the overview isn't —
 * mirrors the Loans/Transactions pages' page+totals split.
 */
@Component({
  selector: 'lm-interest-earned-report',
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
    MatTooltipModule,
    MatDialogModule,
    ChartTooltipDirective,
  ],
  templateUrl: './interest-earned-report.component.html',
  styleUrls: ['./interest-earned-report.component.scss'],
})
export class InterestEarnedReportComponent implements OnInit, OnDestroy {
  displayedColumns = [
    'loanNumber', 'customer', 'loanDate', 'dueDate', 'principal', 'contractInterest', 'extensionInterest',
    'earnedBeforePeriod', 'earnedThisPeriod', 'totalEarned', 'adjustment', 'finalEarned', 'status', 'classification', 'actions',
  ];
  dataSource = new MatTableDataSource<InterestEarnedRow>([]);
  statusLabel = STATUS_LABEL;
  classificationLabel = CLASSIFICATION_LABEL;

  overview: InterestEarnedOverview | null = null;
  barGroups: BarGroup[] = [];
  barChartMax = 0;

  pageIndex = 0;
  pageSize = 25;
  totalCount = 0;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  exportingXlsx = false;
  exportingPdf = false;
  private searchTerm = '';

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  private readonly search$ = new Subject<string>();
  private readonly searchSubscription: Subscription;

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    fromDate: [firstOfMonthLocalDateString()],
    toDate: [todayLocalDateString()],
    status: [null as LoanStatus | null],
    classification: [null as LoanClassification | null],
    interestType: ['all' as InterestType],
  });

  constructor(
    private readonly getInterestEarnedPage: GetInterestEarnedPageUseCase,
    private readonly getInterestEarnedOverview: GetInterestEarnedOverviewUseCase,
    private readonly getInterestEarnedMonthlyChart: GetInterestEarnedMonthlyChartUseCase,
    private readonly exportXlsxUseCase: ExportInterestEarnedXlsxUseCase,
    private readonly exportPdfUseCase: ExportInterestEarnedPdfUseCase,
    private readonly dialog: MatDialog,
    readonly router: Router,
  ) {
    this.searchSubscription = this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.pageIndex = 0;
      if (this.paginator) this.paginator.pageIndex = 0;
      this.loadPage();
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private currentFilters(): InterestEarnedFilters {
    const raw = this.filters.getRawValue();
    return {
      fromDate: raw.fromDate!,
      toDate: raw.toDate!,
      search: this.searchTerm || undefined,
      status: raw.status ?? undefined,
      classification: raw.classification ?? undefined,
      interestType: raw.interestType ?? undefined,
    };
  }

  private loadAll(): void {
    this.loadOverviewAndChart();
    this.loadPage();
  }

  private loadOverviewAndChart(): void {
    const filters = this.currentFilters();
    forkJoin({
      overview: this.getInterestEarnedOverview.execute(filters),
      chart: this.getInterestEarnedMonthlyChart.execute(filters),
    }).subscribe(({ overview, chart }) => {
      this.overview = overview;
      this.buildBarChart(chart);
    });
  }

  private loadPage(): void {
    const filters = this.currentFilters();
    this.getInterestEarnedPage.execute(this.pageIndex, this.pageSize, filters, this.sortBy, this.sortDir).subscribe((result) => {
      this.dataSource.data = result.items;
      this.totalCount = result.totalCount;
    });
  }

  private buildBarChart(months: InterestEarnedMonthlyPoint[]): void {
    const max = Math.max(1, ...months.map((m) => Math.max(m.currentYear, m.previousYear)));
    this.barChartMax = this.niceMax(max);
    const currentYear = new Date().getFullYear();

    this.barGroups = months.map((m) => ({
      month: m.month,
      currentHeight: (m.currentYear / this.barChartMax) * 100,
      previousHeight: (m.previousYear / this.barChartMax) * 100,
      currentTooltip: `${m.month} ${currentYear}: ${fmtMoney(m.currentYear)}`,
      previousTooltip: `${m.month} ${currentYear - 1}: ${fmtMoney(m.previousYear)}`,
    }));
  }

  private niceMax(value: number): number {
    const magnitude = Math.pow(10, Math.floor(Math.log10(Math.max(1, value))));
    return Math.ceil(value / magnitude) * magnitude;
  }

  applyFilters(): void {
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.loadAll();
  }

  clearFilters(): void {
    this.filters.reset({ fromDate: firstOfMonthLocalDateString(), toDate: todayLocalDateString(), status: null, classification: null, interestType: 'all' });
    this.searchTerm = '';
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

  onSort(sort: Sort): void {
    this.sortBy = sort.direction ? sort.active : undefined;
    this.sortDir = sort.direction || undefined;
    this.pageIndex = 0;
    if (this.paginator) this.paginator.pageIndex = 0;
    this.loadPage();
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  getClassificationLabel(classification: LoanClassification): string {
    return this.classificationLabel[classification];
  }

  fmtMoney(n: number): string {
    return fmtMoney(n);
  }

  openLoanDetails(row: InterestEarnedRow): void {
    this.router.navigate(['/loans', row.loanId]);
  }

  openBreakdown(row: InterestEarnedRow): void {
    const filters = this.currentFilters();
    this.dialog.open(InterestEarnedBreakdownDialogComponent, {
      width: '520px',
      maxWidth: '95vw',
      data: { loanId: row.loanId, fromDate: filters.fromDate, toDate: filters.toDate },
    });
  }

  exportExcel(): void {
    if (this.exportingXlsx) return;
    this.exportingXlsx = true;
    this.exportXlsxUseCase.execute(this.currentFilters()).subscribe({
      next: (blob) => this.downloadBlob(blob, `interest_earned_report_${todayLocalDateString()}.xlsx`, () => (this.exportingXlsx = false)),
      error: () => (this.exportingXlsx = false),
    });
  }

  exportPdfFile(): void {
    if (this.exportingPdf) return;
    this.exportingPdf = true;
    this.exportPdfUseCase.execute(this.currentFilters()).subscribe({
      next: (blob) => this.downloadBlob(blob, `interest_earned_report_${todayLocalDateString()}.pdf`, () => (this.exportingPdf = false)),
      error: () => (this.exportingPdf = false),
    });
  }

  private downloadBlob(blob: Blob, filename: string, done: () => void): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 30_000);
    done();
  }
}
