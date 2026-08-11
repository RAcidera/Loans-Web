import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { CashLedgerEntry, CashSummary, CashTransactionType } from '../../domain/entities/cash-ledger-entry.entity';
import { GetCashSummaryUseCase } from '../../application/use-cases/get-cash-summary.use-case';
import { GetCashLedgerUseCase } from '../../application/use-cases/get-cash-ledger.use-case';
import { AddCashTransactionDialogComponent } from '../add-cash-transaction-dialog/add-cash-transaction-dialog.component';
import { AuthService } from '../../application/auth/auth.service';

const TYPE_LABEL: Record<CashTransactionType, string> = {
  loan_release: 'Loan release',
  payment_received: 'Payment received',
  owner_deposit: 'Owner deposit',
  owner_withdrawal: 'Owner withdrawal',
  expense: 'Expense',
};

const CASH_IN_TYPES: CashTransactionType[] = ['payment_received', 'owner_deposit'];

/**
 * Presentation layer — SRS wireframe 0 (Cash / Funds Page): current cash on
 * hand, total revolving funds, an "Add Cash Transaction" button, and the
 * transaction history table.
 */
@Component({
  selector: 'lm-cash-funds',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule],
  templateUrl: './cash-funds.component.html',
  styleUrls: ['./cash-funds.component.scss'],
})
export class CashFundsComponent implements OnInit {
  summary: CashSummary | null = null;
  ledger: CashLedgerEntry[] = [];
  loading = true;

  displayedColumns = ['date', 'type', 'reference', 'amount', 'remarks'];
  typeLabel = TYPE_LABEL;

  constructor(
    private readonly getCashSummary: GetCashSummaryUseCase,
    private readonly getCashLedger: GetCashLedgerUseCase,
    private readonly dialog: MatDialog,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.getCashSummary.execute().subscribe((summary) => {
      this.summary = summary;
      this.loading = false;
    });
    this.getCashLedger.execute().subscribe((entries) => (this.ledger = entries));
  }

  isCashIn(type: CashTransactionType): boolean {
    return CASH_IN_TYPES.includes(type);
  }

  getTypeLabel(type: CashTransactionType): string {
    return this.typeLabel[type];
  }

  sparkBars(sparkline: number[] = []): number[] {
    return sparkline.map((v) => Math.round(v * 100));
  }

  openAddTransaction(): void {
    this.dialog
      .open(AddCashTransactionDialogComponent, { width: '420px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.added) this.load();
      });
  }
}
