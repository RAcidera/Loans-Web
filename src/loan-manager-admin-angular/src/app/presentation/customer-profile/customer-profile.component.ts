import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Customer } from '../../domain/entities/customer.entity';
import { Loan, LoanStatus } from '../../domain/entities/loan.entity';
import { GetCustomerDetailUseCase } from '../../application/use-cases/get-customer-detail.use-case';
import { UpdateCustomerUseCase } from '../../application/use-cases/update-customer.use-case';
import { DocumentManagerComponent } from '../document-manager/document-manager.component';
import { AuthService } from '../../application/auth/auth.service';

const STATUS_LABEL: Record<LoanStatus, string> = {
  active: 'Active',
  extended: 'Extended',
  paid: 'Paid',
  overdue: 'Overdue',
  writtenoff: 'Written Off',
};

interface FinancialKpi {
  label: string;
  value: string;
  icon: string;
  tone: 'neutral' | 'good' | 'warn';
}

/**
 * Presentation layer — SRS wireframe 4 (Customer Profile): profile fields
 * (editable, Admin only), a Financial Summary KPI row (same formulas as the
 * dashboard's, scoped to this customer), and Loans/Documents/Notes tabs.
 * The Loans tab links out to the routed /loans/:id page rather than
 * rebuilding loan detail viewing here.
 */
@Component({
  selector: 'lm-customer-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    DocumentManagerComponent,
  ],
  templateUrl: './customer-profile.component.html',
  styleUrls: ['./customer-profile.component.scss'],
})
export class CustomerProfileComponent implements OnInit {
  customer: Customer | null = null;
  loans: Loan[] = [];
  financialKpis: FinancialKpi[] = [];
  loading = true;
  editing = false;
  saving = false;

  displayedColumns = ['id', 'principal', 'dueDate', 'balance', 'status', 'actions'];
  statusLabel = STATUS_LABEL;

  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private customerId!: string;

  form = this.fb.group({
    fullName: ['', Validators.required],
    address: ['', Validators.required],
    contactNumber: ['', Validators.required],
    borrowerType: ['', Validators.required],
    nicknameAlias: [''],
    notes: [''],
  });

  constructor(
    private readonly getCustomerDetail: GetCustomerDetailUseCase,
    private readonly updateCustomer: UpdateCustomerUseCase,
    private readonly router: Router,
    readonly authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.getCustomerDetail.execute(this.customerId).subscribe(({ customer, loans, receivables }) => {
      this.customer = customer ?? null;
      this.loans = loans;
      this.loading = false;

      this.financialKpis = [
        { label: 'Active loans', value: `${receivables.activeLoansCount}`, icon: 'receipt_long', tone: 'neutral' },
        { label: 'Gross receivables', value: `₱${receivables.grossReceivables.toLocaleString()}`, icon: 'account_balance_wallet', tone: 'neutral' },
        { label: 'Collectible receivables', value: `₱${receivables.collectibleReceivables.toLocaleString()}`, icon: 'savings', tone: 'good' },
        { label: 'Bad loan receivables', value: `₱${receivables.badLoanReceivables.toLocaleString()}`, icon: 'warning_amber', tone: receivables.badLoanReceivables > 0 ? 'warn' : 'neutral' },
      ];
    });
  }

  getStatusLabel(status: LoanStatus): string {
    return this.statusLabel[status];
  }

  startEdit(): void {
    if (!this.customer) return;
    this.form.setValue({
      fullName: this.customer.fullName,
      address: this.customer.address,
      contactNumber: this.customer.contactNumber,
      borrowerType: this.customer.borrowerType,
      nicknameAlias: this.customer.nicknameAlias,
      notes: this.customer.notes,
    });
    this.editing = true;
  }

  cancelEdit(): void {
    this.editing = false;
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const { fullName, address, contactNumber, borrowerType, nicknameAlias, notes } = this.form.getRawValue();
    this.updateCustomer
      .execute(this.customerId, fullName!, address!, contactNumber!, borrowerType!, nicknameAlias ?? '', notes ?? '')
      .subscribe((updated) => {
        this.customer = updated;
        this.saving = false;
        this.editing = false;
      });
  }

  openLoanDetails(loan: Loan): void {
    this.router.navigate(['/loans', loan.loanId]);
  }

  back(): void {
    this.router.navigate(['/customers']);
  }
}
