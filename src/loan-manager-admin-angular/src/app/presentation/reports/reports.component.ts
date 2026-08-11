import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';

import { Loan, LoanStatus } from '../../domain/entities/loan.entity';
import { CustomerSummary, PeriodSummary } from '../../domain/entities/report.entity';
import { GetInterestSummaryUseCase } from '../../application/use-cases/get-interest-summary.use-case';
import { GetCustomerSummaryUseCase } from '../../application/use-cases/get-customer-summary.use-case';
import { GetPeriodSummaryUseCase } from '../../application/use-cases/get-period-summary.use-case';
import { ExportPeriodReportUseCase } from '../../application/use-cases/export-period-report.use-case';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

function toDateString(date: Date): string {
  return date.toISOString().slice(0, 10);
}

/**
 * Presentation layer — SRS wireframe 6 / SRS 3.5 (Reports): total interest
 * earned, a per-customer summary, and a date-range-scoped period summary
 * with an exportable loans table.
 */
@Component({
  selector: 'lm-reports',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
  ],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class ReportsComponent implements OnInit {
  allTimeInterestEarned: number | null = null;
  customerSummaries: CustomerSummary[] = [];
  periodSummary: PeriodSummary | null = null;
  loansInRange: Loan[] = [];
  loadingPeriod = true;
  exporting = false;

  loanColumns = ['id', 'customer', 'principal', 'dueDate', 'balance', 'status'];
  customerColumns = ['customer', 'totalBorrowed', 'totalPaid', 'loansCount'];
  statusLabel = STATUS_LABEL;

  private readonly fb = inject(FormBuilder);
  private allLoans: Loan[] = [];

  range = this.fb.group({
    start: [this.daysAgo(30)],
    end: [new Date()],
  });

  constructor(
    private readonly getInterestSummary: GetInterestSummaryUseCase,
    private readonly getCustomerSummary: GetCustomerSummaryUseCase,
    private readonly getPeriodSummary: GetPeriodSummaryUseCase,
    private readonly exportPeriodReport: ExportPeriodReportUseCase,
    private readonly getLoans: GetLoansUseCase,
  ) {}

  ngOnInit(): void {
    this.getInterestSummary.execute().subscribe((summary) => {
      this.allTimeInterestEarned = summary.totalInterestEarned;
    });

    this.getCustomerSummary.execute().subscribe((summaries) => {
      this.customerSummaries = summaries;
    });

    this.getLoans.execute().subscribe((loans) => {
      this.allLoans = loans;
      this.applyRange();
    });
  }

  applyRange(): void {
    const { start, end } = this.range.getRawValue();
    if (!start || !end) return;

    this.loadingPeriod = true;
    const startStr = toDateString(start);
    const endStr = toDateString(end);

    this.getPeriodSummary.execute(startStr, endStr).subscribe((summary) => {
      this.periodSummary = summary;
      this.loadingPeriod = false;
    });

    this.loansInRange = this.allLoans.filter((l) => l.startDate >= startStr && l.startDate <= endStr);
  }

  export(): void {
    const { start, end } = this.range.getRawValue();
    if (!start || !end) return;

    this.exporting = true;
    const startStr = toDateString(start);
    const endStr = toDateString(end);

    this.exportPeriodReport.execute(startStr, endStr).subscribe((blob) => {
      this.exporting = false;
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `report_${startStr}_${endStr}.csv`;
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  private daysAgo(days: number): Date {
    const date = new Date();
    date.setDate(date.getDate() - days);
    return date;
  }
}
