// Domain layer — the Customers list's KPI strip, counted/summed across the whole filtered result set.

export interface CustomerTotals {
  totalCustomersCount: number;
  activeCustomersCount: number;
  inactiveCustomersCount: number;
  totalLoansCount: number;
  totalOutstandingBalance: number;
}
