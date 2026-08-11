// Domain layer — spec's "Dashboard Receivable Calculations" + "Dashboard Summary Cards".

export interface DashboardReceivables {
  grossReceivables: number;
  collectibleReceivables: number;
  badLoanReceivables: number;
  activeLoansCount: number;
  overdueLoansCount: number;
  writtenOffLoansCount: number;
  loansDueThisWeekCount: number;
}
