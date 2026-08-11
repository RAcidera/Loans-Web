import { Observable } from 'rxjs';
import { Customer } from '../entities/customer.entity';
import { CustomerDocument } from '../entities/customer-document.entity';
import { DashboardReceivables } from '../entities/dashboard-receivables.entity';
import { Loan, LoanClassification, LoanPageFilters } from '../entities/loan.entity';
import { LoanAuditLogEntry } from '../entities/loan-audit-log-entry.entity';
import { LoanDocument } from '../entities/loan-document.entity';
import { LoanExtension } from '../entities/loan-extension.entity';
import { LoanLedgerEntry } from '../entities/loan-ledger-entry.entity';
import { LoanTotals } from '../entities/loan-totals.entity';
import { PagedResult } from '../entities/paged-result.entity';
import { Payment, PaymentMethod, PaymentWithCustomer } from '../entities/payment.entity';

/** Customers list-page row shape — a Customer plus its server-computed loan count. */
export type CustomerListItem = Customer & { loanCount: number };

/** Spec's "Edit Loan" overrides — every field optional so a caller can change just one. */
export interface UpdateLoanFields {
  startDate?: string;
  dueDate?: string;
  interestRate?: number;
  interestAmount?: number;
  remarks?: string;
}

/**
 * Port covering Customers, Loans, Loan_Extensions, and Payments — the four
 * tables in the SRS that revolve around an individual loan's lifecycle.
 * See domain/repositories/cash-ledger.repository.ts for the separate
 * cash/funds boundary.
 */
export abstract class LoanRepository {
  abstract getCustomers(): Observable<Customer[]>;
  /**
   * One page of customers for the Customers list table's server-side
   * paging — leaves getCustomers() untouched since add-loan-dialog's
   * customer dropdown still needs the full unpaged list.
   */
  abstract getCustomersPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
  ): Observable<PagedResult<CustomerListItem>>;
  abstract getCustomerById(customerId: string): Observable<Customer | undefined>;
  /** SRS 3.1 "add ... customer profiles". */
  abstract createCustomer(
    fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias?: string, notes?: string,
  ): Observable<Customer>;
  /** SRS 3.1 "edit ... customer profiles". */
  abstract updateCustomer(
    customerId: string, fullName: string, address: string, contactNumber: string, borrowerType: string,
    nicknameAlias?: string, notes?: string,
  ): Observable<Customer>;

  /** Spec 3.1 "Customer Documents Management" — metadata list, never the file bytes (see downloadCustomerDocument). */
  abstract getCustomerDocuments(customerId: string): Observable<CustomerDocument[]>;
  abstract uploadCustomerDocument(customerId: string, file: File): Observable<CustomerDocument>;
  abstract downloadCustomerDocument(customerId: string, documentId: string): Observable<Blob>;
  abstract deleteCustomerDocument(customerId: string, documentId: string): Observable<void>;

  abstract getLoans(): Observable<Loan[]>;
  /**
   * One page of loans for the Loans list table's server-side paging —
   * leaves getLoans() untouched since Dashboard and Reports still need the
   * full unpaged list.
   */
  abstract getLoansPage(
    pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc',
    filters?: LoanPageFilters,
  ): Observable<PagedResult<Loan>>;
  /** Spec's "Loan Grid Footer Totals" — same filters as getLoansPage, no paging. */
  abstract getLoansTotals(search: string, filters?: LoanPageFilters): Observable<LoanTotals>;
  abstract getLoanById(loanId: string): Observable<Loan | undefined>;
  abstract getLoansByCustomer(customerId: string): Observable<Loan[]>;
  /** Spec's "Dashboard Receivable Calculations" + "Dashboard Summary Cards". */
  abstract getDashboardReceivables(): Observable<DashboardReceivables>;
  /** Same formulas as getDashboardReceivables(), scoped to one customer — Customer Profile's "Financial Summary" card. */
  abstract getCustomerReceivables(customerId: string): Observable<DashboardReceivables>;
  /** SRS 3.2 "originate loans". InterestRate/TermDays/StartDate are optional — the backend applies its own defaults (3%, 60 days, today) when omitted. */
  abstract createLoan(customerId: string, principal: number, interestRate?: number, termDays?: number, startDate?: string): Observable<Loan>;

  /** Spec's "Edit Loan" button — overrides Loan Date/Due Date/Interest Rate/Interest Amount/Remarks post-creation. */
  abstract updateLoan(loanId: string, fields: UpdateLoanFields): Observable<Loan>;
  /** Spec's "Change Classification" button on the Loan Details page — the lender's manual risk judgment, independent of Status. */
  abstract changeLoanClassification(loanId: string, classification: LoanClassification): Observable<Loan>;

  abstract getExtensions(loanId: string): Observable<LoanExtension[]>;
  /** Extends a loan's due date and recalculates its balance, per SRS 3.3. */
  abstract extendLoan(loanId: string, extensionDays: number, additionalInterestAmount: number, remarks: string, additionalChargesAmount?: number): Observable<Loan>;
  /** Edits an extension, rolling its old contribution out of DueDate/TotalInterest/TotalExtensionCharges first. */
  abstract updateExtension(
    loanId: string, extensionId: string, extensionDays: number, additionalInterestAmount: number,
    remarks: string, additionalChargesAmount?: number,
  ): Observable<LoanExtension>;
  /** Removes an extension, reverting DueDate/TotalInterest/TotalExtensionCharges — returns the updated Loan since the extension is gone. */
  abstract deleteExtension(loanId: string, extensionId: string): Observable<Loan>;

  /** Most recent payments across all loans, newest first — powers the dashboard feed (SRS wireframe 2). */
  abstract getRecentPayments(limit: number): Observable<PaymentWithCustomer[]>;

  abstract getPayments(loanId: string): Observable<Payment[]>;
  /**
   * Records a payment and updates the loan balance, per SRS 3.4. Per the
   * SRS, this must also create a `payment_received` entry in the cash
   * ledger — that cross-boundary side effect is the infrastructure
   * implementation's responsibility, not this port's.
   */
  abstract recordPayment(loanId: string, amountPaid: number, paymentMethod: PaymentMethod, notes: string, referenceNumber?: string): Observable<Payment>;
  /** Edits a payment, rolling its old AmountPaid out of TotalPaid/Balance before applying the new one. */
  abstract updatePayment(
    loanId: string, paymentId: string, amountPaid: number, paymentMethod: PaymentMethod,
    notes?: string, referenceNumber?: string,
  ): Observable<Payment>;
  /** Removes a payment, rolling it back out of TotalPaid/Balance — returns the updated Loan since the payment is gone. */
  abstract deletePayment(loanId: string, paymentId: string): Observable<Loan>;

  /** Loan Details "Documents" tab — metadata list, never the file bytes (see downloadLoanDocument). */
  abstract getLoanDocuments(loanId: string): Observable<LoanDocument[]>;
  abstract uploadLoanDocument(loanId: string, file: File): Observable<LoanDocument>;
  abstract downloadLoanDocument(loanId: string, documentId: string): Observable<Blob>;
  abstract deleteLoanDocument(loanId: string, documentId: string): Observable<void>;

  /** The SRS's "Additional Recommendation: Loan Ledger" — backs the Payments/Extensions tabs' Running Balance columns. */
  abstract getLoanLedger(loanId: string): Observable<LoanLedgerEntry[]>;
  /** Loan Details "Audit Log" tab — who changed what, and when. */
  abstract getLoanAuditLog(loanId: string): Observable<LoanAuditLogEntry[]>;

  /** Spec's Statement of Account PDF — the Loan Details "Generate SOA" button. */
  abstract downloadLoanSoa(loanId: string): Observable<Blob>;
}
