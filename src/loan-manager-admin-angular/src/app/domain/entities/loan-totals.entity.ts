// Domain layer — spec's "Loan Grid Footer Totals" plus the Loans list's KPI
// strip, both summed/counted across the whole filtered result set.

export interface LoanTotals {
  totalPrincipal: number;
  totalInterest: number;
  totalExtensionCharges: number;
  totalPayments: number;
  totalOutstandingBalance: number;
  totalLoansCount: number;
  activeLoansCount: number;
  overdueLoansCount: number;
  paidLoansCount: number;
}
