import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Subject, Subscription, debounceTime, distinctUntilChanged, forkJoin } from 'rxjs';

import { DiaryCategory, DiaryEntry, DiarySummary } from '../../domain/entities/diary-entry.entity';
import { Customer } from '../../domain/entities/customer.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { SearchDiaryEntriesUseCase } from '../../application/use-cases/search-diary-entries.use-case';
import { GetDiaryCategoriesUseCase } from '../../application/use-cases/get-diary-categories.use-case';
import { GetDiarySummaryUseCase } from '../../application/use-cases/get-diary-summary.use-case';
import { DeleteDiaryEntryUseCase } from '../../application/use-cases/delete-diary-entry.use-case';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';
import { DiaryFormDialogComponent } from '../diary-form-dialog/diary-form-dialog.component';
import { PromiseFormDialogComponent } from '../promise-form-dialog/promise-form-dialog.component';
import { DiaryHeaderComponent } from '../diary-header/diary-header.component';
import { DiarySummaryComponent } from '../diary-summary/diary-summary.component';
import { DiaryEntryCardComponent } from '../diary-entry-card/diary-entry-card.component';
import { DiaryMiniCalendarComponent } from '../diary-mini-calendar/diary-mini-calendar.component';
import { DiaryCategorySummaryComponent } from '../diary-category-summary/diary-category-summary.component';
import { DiaryQuickSummaryComponent } from '../diary-quick-summary/diary-quick-summary.component';
import { ConfirmDialogService } from '../confirm-dialog/confirm-dialog.service';

interface DiaryDayGroup {
  /** "yyyy-MM-dd" of the group, used only as a trackBy/grouping key. */
  date: string;
  /** "TODAY — AUGUST 15, 2026" / "AUGUST 14, 2026" per requirements §11's wireframe. */
  heading: string;
  entries: DiaryEntry[];
}

/**
 * Presentation layer — requirements diary-modern §2's layout: header, compact
 * filter toolbar, summary cards, then a two-column area (timeline left,
 * supporting panels right). Entries are fetched already sorted
 * EntryDateTime DESC by the backend; this component only groups the flat
 * list into day sections for display and owns the filter/sidebar state that
 * feeds that fetch.
 */
@Component({
  selector: 'lm-diary-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatInputModule,
    MatDialogModule,
    DiaryHeaderComponent,
    DiarySummaryComponent,
    DiaryEntryCardComponent,
    DiaryMiniCalendarComponent,
    DiaryCategorySummaryComponent,
    DiaryQuickSummaryComponent,
  ],
  templateUrl: './diary-list.component.html',
  styleUrls: ['./diary-list.component.scss'],
})
export class DiaryListComponent implements OnInit, OnDestroy {
  groups: DiaryDayGroup[] = [];
  categories: DiaryCategory[] = [];
  customers: Customer[] = [];
  loans: Loan[] = [];
  summary: DiarySummary | null = null;
  loading = true;

  customerFilterText = '';
  loanFilterText = '';
  private loansById = new Map<string, Loan>();

  private searchTerm = '';
  private readonly search$ = new Subject<string>();
  private readonly searchSubscription: Subscription;

  private readonly fb = inject(FormBuilder);
  filters = this.fb.group({
    categoryId: [null as string | null],
    dateFrom: [null as string | null],
    dateTo: [null as string | null],
    customerId: [null as string | null],
    loanId: [null as string | null],
  });

  constructor(
    private readonly searchEntries: SearchDiaryEntriesUseCase,
    private readonly getCategories: GetDiaryCategoriesUseCase,
    private readonly getSummary: GetDiarySummaryUseCase,
    private readonly deleteEntry: DeleteDiaryEntryUseCase,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly getLoans: GetLoansUseCase,
    private readonly dialog: MatDialog,
    private readonly router: Router,
    private readonly confirmDialog: ConfirmDialogService,
  ) {
    this.searchSubscription = this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      this.searchTerm = term;
      this.load();
    });
  }

  ngOnInit(): void {
    forkJoin({
      categories: this.getCategories.execute(),
      customers: this.getCustomers.execute(),
      loans: this.getLoans.execute(),
    }).subscribe(({ categories, customers, loans }) => {
      this.categories = categories;
      this.customers = customers;
      this.loans = loans;
      this.loansById = new Map(loans.map((l) => [l.loanId, l]));
    });

    this.load();
    this.loadSummary();
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }

  private load(): void {
    this.loading = true;
    const raw = this.filters.getRawValue();

    this.searchEntries
      .execute({
        search: this.searchTerm || undefined,
        categoryId: raw.categoryId ?? undefined,
        dateFrom: raw.dateFrom ?? undefined,
        dateTo: raw.dateTo ?? undefined,
        customerId: raw.customerId ?? undefined,
        loanId: raw.loanId ?? undefined,
      })
      .subscribe((entries) => {
        this.groups = this.groupByDay(entries);
        this.loading = false;
      });
  }

  private loadSummary(): void {
    this.getSummary.execute().subscribe((summary) => (this.summary = summary));
  }

  private groupByDay(entries: DiaryEntry[]): DiaryDayGroup[] {
    const todayKey = new Date().toISOString().slice(0, 10);
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const yesterdayKey = yesterday.toISOString().slice(0, 10);
    const groups: DiaryDayGroup[] = [];

    for (const entry of entries) {
      const key = entry.entryDate;
      let group = groups.find((g) => g.date === key);
      if (!group) {
        const label = new Date(`${key}T00:00:00`).toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' }).toUpperCase();
        const prefix = key === todayKey ? 'TODAY — ' : key === yesterdayKey ? 'YESTERDAY — ' : '';
        group = { date: key, heading: `${prefix}${label}`, entries: [] };
        groups.push(group);
      }
      group.entries.push(entry);
    }

    return groups;
  }

  applyFilters(): void {
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({ categoryId: null, dateFrom: null, dateTo: null, customerId: null, loanId: null });
    this.customerFilterText = '';
    this.loanFilterText = '';
    this.searchTerm = '';
    this.load();
  }

  applySearch(value: string): void {
    this.search$.next(value.trim());
  }

  hasActiveFilters(): boolean {
    const raw = this.filters.getRawValue();
    return !!(this.searchTerm || raw.categoryId || raw.dateFrom || raw.dateTo || raw.customerId || raw.loanId);
  }

  // --- Customer/loan searchable dropdowns (requirements diary-modern §4) ---

  get filteredCustomers(): Customer[] {
    const text = this.customerFilterText.trim().toLowerCase();
    const pool = text ? this.customers.filter((c) => c.fullName.toLowerCase().includes(text)) : this.customers;
    return pool.slice(0, 50);
  }

  get filteredLoans(): Loan[] {
    const selectedCustomerId = this.filters.value.customerId;
    let pool = selectedCustomerId ? this.loans.filter((l) => l.customerId === selectedCustomerId) : this.loans;
    const text = this.loanFilterText.trim().toLowerCase();
    if (text) pool = pool.filter((l) => l.loanNumber.toLowerCase().includes(text) || l.customerName.toLowerCase().includes(text));
    return pool.slice(0, 50);
  }

  displayCustomerName = (customerId: string | null | undefined): string => this.customers.find((c) => c.customerId === customerId)?.fullName ?? '';

  displayLoanNumber = (loanId: string | null | undefined): string => {
    const loan = this.loans.find((l) => l.loanId === loanId);
    return loan ? `${loan.loanNumber} — ${loan.customerName}` : '';
  };

  onCustomerSelected(event: MatAutocompleteSelectedEvent): void {
    this.filters.patchValue({ customerId: event.option.value, loanId: null });
    this.loanFilterText = '';
  }

  onLoanSelected(event: MatAutocompleteSelectedEvent): void {
    this.filters.patchValue({ loanId: event.option.value });
  }

  clearCustomer(): void {
    this.customerFilterText = '';
    this.filters.patchValue({ customerId: null, loanId: null });
  }

  clearLoan(): void {
    this.loanFilterText = '';
    this.filters.patchValue({ loanId: null });
  }

  // --- Right sidebar wiring ---

  onCategorySelected(categoryId: string | null): void {
    this.filters.patchValue({ categoryId });
    this.load();
  }

  onDateSelected(date: string): void {
    this.filters.patchValue({ dateFrom: date || null, dateTo: date || null });
    this.load();
  }

  // --- Entry navigation/actions ---

  loanFor(entry: DiaryEntry): Loan | undefined {
    return entry.loanId ? this.loansById.get(entry.loanId) : undefined;
  }

  openEntry(entry: DiaryEntry): void {
    this.router.navigate(['/diary', entry.diaryEntryId]);
  }

  openNewEntry(): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  openNewReminder(): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { presetReminder: true } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  openNewPromise(): void {
    this.dialog
      .open(PromiseFormDialogComponent, { width: '480px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  editEntry(entry: DiaryEntry): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { editing: entry } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  duplicateEntry(entry: DiaryEntry): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { duplicateFrom: entry } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  addReminderTo(entry: DiaryEntry): void {
    this.dialog
      .open(DiaryFormDialogComponent, { width: '660px', maxWidth: '95vw', data: { editing: entry, presetReminder: true } })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  createFollowupFrom(entry: DiaryEntry): void {
    this.dialog
      .open(DiaryFormDialogComponent, {
        width: '560px',
        maxWidth: '95vw',
        data: { customerId: entry.customerId, loanId: entry.loanId, presetReminder: true },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result?.saved) this.refreshAll();
      });
  }

  async removeEntry(entry: DiaryEntry): Promise<void> {
    const ok = await this.confirmDialog.confirm({
      title: 'Delete diary entry?',
      message: `Delete "${entry.title}"? This cannot be undone.`,
      confirmText: 'Yes, delete',
    });
    if (!ok) return;
    this.deleteEntry.execute(entry.diaryEntryId).subscribe(() => this.refreshAll());
  }

  private refreshAll(): void {
    this.load();
    this.loadSummary();
  }
}
