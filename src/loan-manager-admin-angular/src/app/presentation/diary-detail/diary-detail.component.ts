import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { DiaryAuditLogEntry, DiaryEntry, FinancialComparison } from '../../domain/entities/diary-entry.entity';
import { GetDiaryEntryUseCase } from '../../application/use-cases/get-diary-entry.use-case';
import { GetDiaryAuditLogUseCase } from '../../application/use-cases/get-diary-audit-log.use-case';
import { CompareSnapshotToTodayUseCase } from '../../application/use-cases/compare-snapshot-to-today.use-case';
import { DeleteDiaryEntryUseCase } from '../../application/use-cases/delete-diary-entry.use-case';
import { DiaryFormDialogComponent } from '../diary-form-dialog/diary-form-dialog.component';
import { CategoryBadgeComponent } from '../category-badge/category-badge.component';
import { FinancialSnapshotComponent } from '../financial-snapshot/financial-snapshot.component';
import { FinancialComparisonComponent } from '../financial-comparison/financial-comparison.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';
import { parseTags } from '../shared/tags-utils';

/** Requirements §13 — the dedicated Diary Entry Detail page (not a modal, per requirements §25). */
@Component({
  selector: 'lm-diary-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    CategoryBadgeComponent,
    FinancialSnapshotComponent,
    FinancialComparisonComponent,
  ],
  templateUrl: './diary-detail.component.html',
  styleUrls: ['./diary-detail.component.scss'],
})
export class DiaryDetailComponent implements OnInit {
  entry: DiaryEntry | null = null;
  auditLog: DiaryAuditLogEntry[] = [];
  comparison: FinancialComparison | null = null;
  comparing = false;
  loading = true;

  private diaryEntryId!: string;

  get tags(): string[] {
    return parseTags(this.entry?.tags);
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly getEntry: GetDiaryEntryUseCase,
    private readonly getAuditLog: GetDiaryAuditLogUseCase,
    private readonly compareToToday: CompareSnapshotToTodayUseCase,
    private readonly deleteEntry: DeleteDiaryEntryUseCase,
    private readonly dialog: MatDialog,
    private readonly confirmDialog: ConfirmDialogService,
  ) {}

  ngOnInit(): void {
    this.diaryEntryId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.getEntry.execute(this.diaryEntryId).subscribe((entry) => {
      this.entry = entry;
      this.loading = false;
    });
    this.getAuditLog.execute(this.diaryEntryId).subscribe((log) => (this.auditLog = log));
  }

  loadComparison(): void {
    if (!this.entry?.snapshot) return;
    this.comparing = true;
    this.compareToToday.execute(this.diaryEntryId).subscribe((comparison) => {
      this.comparison = comparison;
      this.comparing = false;
    });
  }

  editEntry(): void {
    if (!this.entry) return;
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { editing: this.entry } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.load();
      });
  }

  async removeEntry(): Promise<void> {
    if (!this.entry) return;
    const ok = await this.confirmDialog.confirm({
      title: 'Delete diary entry?',
      message: `Delete "${this.entry.title}"? This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
    this.deleteEntry.execute(this.diaryEntryId).subscribe(() => this.router.navigate(['/diary']));
  }

  goBack(): void {
    this.router.navigate(['/diary']);
  }

  openCustomer(): void {
    if (this.entry?.customerId) this.router.navigate(['/customers', this.entry.customerId]);
  }

  openLoan(): void {
    if (this.entry?.loanId) this.router.navigate(['/loans', this.entry.loanId]);
  }
}
