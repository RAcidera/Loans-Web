// Domain layer — mirrors the Loans table in the SRS.
// Fixed-term model: a loan has one start date and one due date (default 60
// days), a flat interest rate (default 3%), and can move to "extended" via
// a LoanExtension rather than being restructured into a new schedule.

export type LoanStatus = 'active' | 'extended' | 'paid' | 'overdue' | 'writtenoff';

/** User-managed risk judgment — separate from LoanStatus, which is system-managed. */
export type LoanClassification = 'normal' | 'watchlist' | 'badloan';

export interface Loan {
  loanId: string;
  /** Human-friendly sequential code (e.g. "LOA00001") — display this, never loanId's raw GUID. */
  loanNumber: string;
  customerId: string;
  customerName: string;
  principalAmount: number;
  interestRate: number; // e.g. 0.03 for 3%, flat, not compounding
  startDate: string;
  dueDate: string; // current due date — pushed out by extensions
  totalInterest: number;
  /** Sum of every extension's additional-charges fee — kept separate from interest, see Outstanding Balance formula. */
  totalExtensionCharges: number;
  totalAmountDue: number;
  totalPaid: number;
  balance: number; // totalAmountDue - totalPaid
  status: LoanStatus;
  classification: LoanClassification;
  /** Free-text notes on the loan — editable via EditLoanUseCase after creation. */
  remarks: string;
  createdAt: string;
}

/** Spec's "Loan Search and Filtering" — passed to LoanRepository.getLoansPage()/getLoansTotals() alongside the free-text search. */
export interface LoanPageFilters {
  status?: LoanStatus;
  classification?: LoanClassification;
  loanDateFrom?: string;
  loanDateTo?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  badLoansOnly?: boolean;
  overdueOnly?: boolean;
}
