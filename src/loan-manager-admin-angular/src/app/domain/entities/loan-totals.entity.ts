// Domain layer — spec's "Loan Grid Footer Totals", summed across the whole filtered result set.

export interface LoanTotals {
  totalPrincipal: number;
  totalInterest: number;
  totalExtensionCharges: number;
  totalPayments: number;
  totalOutstandingBalance: number;
}
