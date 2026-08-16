import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { InterestEarnedLoanBreakdown } from '../../domain/entities/interest-earned-report.entity';
import { GetInterestEarnedLoanBreakdownUseCase } from '../../application/use-cases/get-interest-earned-loan-breakdown.use-case';

export interface InterestEarnedBreakdownDialogData {
  loanId: string;
  fromDate: string;
  toDate: string;
}

function fmtMoney(n: number): string {
  const sign = n < 0 ? '-' : '';
  return `${sign}₱${Math.abs(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * The Loan Interest Drill-Down (report spec §18) — "how did the system
 * arrive at this number," one card per earning period (original loan +
 * each extension), each showing its own term/daily-rate/earned-days math.
 */
@Component({
  selector: 'lm-interest-earned-breakdown-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './interest-earned-breakdown-dialog.component.html',
  styleUrls: ['./interest-earned-breakdown-dialog.component.scss'],
})
export class InterestEarnedBreakdownDialogComponent implements OnInit {
  loading = true;
  breakdown: InterestEarnedLoanBreakdown | null = null;

  constructor(
    private readonly dialogRef: MatDialogRef<InterestEarnedBreakdownDialogComponent>,
    @Inject(MAT_DIALOG_DATA) private readonly data: InterestEarnedBreakdownDialogData,
    private readonly getBreakdown: GetInterestEarnedLoanBreakdownUseCase,
  ) {}

  ngOnInit(): void {
    this.getBreakdown.execute(this.data.loanId, this.data.fromDate, this.data.toDate).subscribe((breakdown) => {
      this.breakdown = breakdown;
      this.loading = false;
    });
  }

  fmtMoney(n: number): string {
    return fmtMoney(n);
  }

  close(): void {
    this.dialogRef.close();
  }
}
