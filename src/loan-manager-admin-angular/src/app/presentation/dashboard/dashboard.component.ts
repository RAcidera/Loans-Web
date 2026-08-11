import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { forkJoin } from 'rxjs';

import { Loan, LoanStatus } from '../../domain/entities/loan.entity';
import { PaymentWithCustomer } from '../../domain/entities/payment.entity';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';
import { GetCashSummaryUseCase } from '../../application/use-cases/get-cash-summary.use-case';
import { GetRecentPaymentsUseCase } from '../../application/use-cases/get-recent-payments.use-case';
import { GetDashboardReceivablesUseCase } from '../../application/use-cases/get-dashboard-receivables.use-case';

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

interface DashboardKpi {
  label: string;
  value: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn';
  sparkline?: number[];
}

/**
 * Presentation layer. Composes three use cases (loans, cash summary,
 * recent payments) into the KPI cards and feed the SRS's dashboard
 * wireframe calls for — that composition is a presentation-layer concern;
 * none of it lives in the use cases themselves.
 */
@Component({
  selector: 'lm-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatIconModule,
    MatButtonModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, AfterViewInit {
  kpis: DashboardKpi[] = [];
  recentPayments: PaymentWithCustomer[] = [];

  displayedColumns = ['id', 'customer', 'principal', 'dueDate', 'balance', 'status', 'actions'];
  dataSource = new MatTableDataSource<Loan>([]);
  statusLabel = STATUS_LABEL;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(
    private readonly getLoans: GetLoansUseCase,
    private readonly getCashSummary: GetCashSummaryUseCase,
    private readonly getRecentPayments: GetRecentPaymentsUseCase,
    private readonly getDashboardReceivables: GetDashboardReceivablesUseCase,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({
      loans: this.getLoans.execute(),
      cash: this.getCashSummary.execute(),
      receivables: this.getDashboardReceivables.execute(),
    }).subscribe(({ loans, cash, receivables }) => {
      this.dataSource.data = loans;

      this.kpis = [
        { label: 'Gross receivables', value: `₱${receivables.grossReceivables.toLocaleString()}`, icon: 'account_balance_wallet', tone: 'neutral' },
        { label: 'Collectible receivables', value: `₱${receivables.collectibleReceivables.toLocaleString()}`, icon: 'savings', tone: 'good' },
        { label: 'Bad loan receivables', value: `₱${receivables.badLoanReceivables.toLocaleString()}`, icon: 'warning_amber', tone: receivables.badLoanReceivables > 0 ? 'warn' : 'neutral' },
        { label: 'Active loans', value: `${receivables.activeLoansCount}`, icon: 'receipt_long', tone: 'neutral' },
        { label: 'Overdue loans', value: `${receivables.overdueLoansCount}`, icon: 'error_outline', tone: receivables.overdueLoansCount > 0 ? 'warn' : 'neutral' },
        { label: 'Written-off loans', value: `${receivables.writtenOffLoansCount}`, icon: 'block', tone: 'neutral' },
        { label: 'Loans due this week', value: `${receivables.loansDueThisWeekCount}`, icon: 'event_upcoming', tone: receivables.loansDueThisWeekCount > 0 ? 'warn' : 'neutral' },
        { label: 'Cash on hand', value: `₱${cash.cashOnHand.toLocaleString()}`, icon: 'payments', tone: 'good', sparkline: cash.sevenDayTrend },
        { label: 'Revolving funds available', value: `₱${cash.revolvingFunds.toLocaleString()}`, icon: 'sync_alt', tone: 'good', sparkline: cash.sevenDayTrend },
      ];
    });

    this.getRecentPayments.execute(6).subscribe((payments) => (this.recentPayments = payments));
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  sparkBars(sparkline: number[] = []): number[] {
    return sparkline.map((v) => Math.round(v * 100));
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  openLoanDetails(loan: Loan): void {
    this.router.navigate(['/loans', loan.loanId]);
  }
}
