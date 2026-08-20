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
  /** Lender's collection schedule in months (1-12) — most customers are 2 months (60 days) paid daily; some weekly/biweekly/monthly. */
  paymentTermsMonths: number;
  totalInterest: number;
  /** Sum of every extension's additional-charges fee — kept separate from interest, see Outstanding Balance formula. */
  totalExtensionCharges: number;
  totalAmountDue: number;
  totalPaid: number;
  balance: number; // totalAmountDue - totalPaid
  /** Computed server-side: (principalAmount + totalInterest) / (paymentTermsMonths * 30) — the suggested default amount when recording a payment. */
  dailyPayment: number;
  status: LoanStatus;
  classification: LoanClassification;
  /** Free-text notes on the loan — editable via EditLoanUseCase after creation. */
  remarks: string;
  createdAt: string;
  /** Only populated by GetLoanDetailUseCase (the Loan Details page's "Contact" stat) — every other loan-list fetch omits it. */
  customerContactNumber?: string;
  /** dueDate - business-local today, computed server-side (negative once overdue). Only populated by GetLoanDetailUseCase — see LoanDto.DaysUntilDue on the backend for why this isn't recomputed client-side from the browser's clock. */
  daysUntilDue?: number;
}

/**
 * Spec's "Loan Search and Filtering" — passed to
 * LoanRepository.getLoansPage()/getLoansTotals() alongside the free-text
 * search. statuses/classifications are arrays — the Loans list allows
 * selecting more than one of each at once (e.g. Active + Extended) — sent
 * to the backend as a single comma-separated query param; an empty/absent
 * array means "no filter on this field".
 */
export interface LoanPageFilters {
  statuses?: LoanStatus[];
  classifications?: LoanClassification[];
  loanDateFrom?: string;
  loanDateTo?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  badLoansOnly?: boolean;
  overdueOnly?: boolean;
}
