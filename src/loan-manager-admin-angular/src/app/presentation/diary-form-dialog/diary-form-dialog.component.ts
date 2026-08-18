import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule, MatChipInputEvent } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { forkJoin } from 'rxjs';

import { DiaryCategory, DiaryEntry } from '../../domain/entities/diary-entry.entity';
import { Customer } from '../../domain/entities/customer.entity';
import { Loan } from '../../domain/entities/loan.entity';
import { GetDiaryCategoriesUseCase } from '../../application/use-cases/get-diary-categories.use-case';
import { CreateDiaryEntryUseCase } from '../../application/use-cases/create-diary-entry.use-case';
import { UpdateDiaryEntryUseCase } from '../../application/use-cases/update-diary-entry.use-case';
import { GetCustomersUseCase } from '../../application/use-cases/get-customers.use-case';
import { GetLoansUseCase } from '../../application/use-cases/get-loans.use-case';
import { todayLocalDateString, nowLocalTimeString } from '../shared/date-utils';
import { parseTags, joinTags } from '../shared/tags-utils';

export interface DiaryFormDialogData {
  /** When set, the dialog edits this existing entry instead of creating a new one. */
  editing?: DiaryEntry;
  /** When set (and `editing` isn't), prefills title/category/notes/tags/customer/loan from this entry but always creates a NEW entry with today's date/time — backs the entry card's "Duplicate" action. */
  duplicateFrom?: DiaryEntry;
  /** Preselects the Customer/Loan link when opened from a Customer or Loan page — ignored when editing. */
  customerId?: string;
  loanId?: string;
  /** Pre-checks "Add reminder" — used by the Calendar's "+ New" → "Reminder / Follow-up" menu item, and the entry card's "Add Reminder"/"Create Follow-up" actions, all of which are otherwise identical to "Diary Entry". */
  presetReminder?: boolean;
}

/** Requirements §6 — shared between "New Diary Entry" and editing an existing one; the fields are identical, only the submit action and the (edit-only-hidden) snapshot checkbox differ. */
@Component({
  selector: 'lm-diary-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
  ],
  templateUrl: './diary-form-dialog.component.html',
  styleUrls: ['./diary-form-dialog.component.scss'],
})
export class DiaryFormDialogComponent implements OnInit {
  submitting = false;
  error: string | null = null;

  categories: DiaryCategory[] = [];
  customers: Customer[] = [];
  /** Narrowed to the selected Customer's loans (or every loan, when none is selected) — see onCustomerChange. */
  loans: Loan[] = [];
  tags: string[] = [];
  private allLoans: Loan[] = [];

  // MatDialogConfig.data defaults to null when a caller opens this dialog
  // without a `data` option (e.g. diary-list's "New Entry" button, which
  // has nothing to prefill) — every field on DiaryFormDialogData is
  // optional, so `{}` is a valid fallback and keeps the reads below from
  // throwing on a null MAT_DIALOG_DATA.
  readonly data: DiaryFormDialogData = inject(MAT_DIALOG_DATA) ?? {};
  readonly editing = this.data.editing;
  /** Prefill source for title/category/notes/tags/customer/loan — the entry being edited, or the entry being duplicated. Duplicating never copies entryDate/entryTime/reminder. */
  private readonly prefill = this.editing ?? this.data.duplicateFrom;
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    title: [this.prefill?.title ?? '', Validators.required],
    categoryId: [this.prefill?.categoryId ?? '', Validators.required],
    entryDate: [this.editing?.entryDate ?? todayLocalDateString(), Validators.required],
    entryTime: [this.editing?.entryTime ?? nowLocalTimeString(), Validators.required],
    notes: [this.prefill?.notes ?? ''],
    captureFinancialSnapshot: [{ value: false, disabled: !!this.editing }],
    customerId: [this.editing?.customerId ?? this.prefill?.customerId ?? this.data.customerId ?? ''],
    loanId: [this.editing?.loanId ?? this.prefill?.loanId ?? this.data.loanId ?? ''],
    addReminder: [!!this.editing?.reminderDate || !!this.data.presetReminder],
    reminderDate: [this.editing?.reminderDate ?? ''],
    reminderTime: [this.editing?.reminderTime ?? ''],
  });

  constructor(
    private readonly dialogRef: MatDialogRef<DiaryFormDialogComponent>,
    private readonly getCategories: GetDiaryCategoriesUseCase,
    private readonly createEntry: CreateDiaryEntryUseCase,
    private readonly updateEntry: UpdateDiaryEntryUseCase,
    private readonly getCustomers: GetCustomersUseCase,
    private readonly getLoans: GetLoansUseCase,
  ) {
    this.tags = parseTags(this.prefill?.tags);
  }

  ngOnInit(): void {
    forkJoin({
      categories: this.getCategories.execute(),
      customers: this.getCustomers.execute(),
      loans: this.getLoans.execute(),
    }).subscribe(({ categories, customers, loans }) => {
      this.categories = categories;
      this.customers = customers;
      this.allLoans = loans;
      this.refreshLoanOptions(this.form.value.customerId ?? '');
    });
  }

  /** Requirements §4-style behavior (already used by the Promise to Pay dialog) — picking a Customer narrows Loan to just that customer's loans, and clears any previously selected Loan that no longer applies. */
  onCustomerChange(customerId: string): void {
    this.refreshLoanOptions(customerId);
    this.form.patchValue({ loanId: '' });
  }

  private refreshLoanOptions(customerId: string): void {
    this.loans = customerId ? this.allLoans.filter((l) => l.customerId === customerId) : this.allLoans;
  }

  addTag(event: MatChipInputEvent): void {
    const value = (event.value ?? '').trim();
    if (value && !this.tags.includes(value)) this.tags.push(value);
    event.chipInput?.clear();
  }

  removeTag(tag: string): void {
    this.tags = this.tags.filter((t) => t !== tag);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.error = null;

    const raw = this.form.getRawValue();
    const reminderDate = raw.addReminder && raw.reminderDate ? raw.reminderDate : undefined;
    const reminderTime = raw.addReminder && raw.reminderTime ? raw.reminderTime : undefined;
    const tags = joinTags(this.tags);

    const result$ = this.editing
      ? this.updateEntry.execute(this.editing.diaryEntryId, {
          title: raw.title!,
          categoryId: raw.categoryId!,
          notes: raw.notes ?? '',
          customerId: raw.customerId || undefined,
          loanId: raw.loanId || undefined,
          entryDate: raw.entryDate!,
          entryTime: raw.entryTime!,
          reminderDate,
          reminderTime,
          tags,
        })
      : this.createEntry.execute({
          title: raw.title!,
          categoryId: raw.categoryId!,
          notes: raw.notes ?? '',
          captureFinancialSnapshot: !!raw.captureFinancialSnapshot,
          customerId: raw.customerId || undefined,
          loanId: raw.loanId || undefined,
          entryDate: raw.entryDate!,
          entryTime: raw.entryTime!,
          reminderDate,
          reminderTime,
          tags,
        });

    result$.subscribe({
      next: (entry) => {
        this.dialogRef.close({ saved: true, entry });
      },
      error: () => {
        this.submitting = false;
        this.error = 'Could not save this diary entry. Please try again.';
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
