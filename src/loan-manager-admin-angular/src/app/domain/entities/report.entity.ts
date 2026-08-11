// Domain layer — mirrors SRS 3.5's Reports page: aggregate views the
// dashboard and cash-funds pages don't already cover.

export interface InterestSummary {
  totalInterestEarned: number;
}

export interface CustomerSummary {
  customerId: string;
  customerName: string;
  totalBorrowed: number;
  totalPaid: number;
  loansCount: number;
}

export interface PeriodSummary {
  startDate: string;
  endDate: string;
  loansOriginated: number;
  paymentsCollected: number;
  extensionsGranted: number;
  interestEarned: number;
}
