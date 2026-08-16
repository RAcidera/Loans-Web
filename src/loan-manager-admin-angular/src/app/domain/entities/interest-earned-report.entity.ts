// Domain layer — Interest Earned Report: elapsed-time ("earned") interest,
// independent of payment timing. Mirrors the backend's
// InterestEarnedRowDto/InterestEarnedOverviewDto/InterestEarnedMonthlyPointDto/
// InterestEarnedLoanBreakdownDto field-for-field.

import { LoanClassification, LoanStatus } from './loan.entity';

export type InterestType = 'all' | 'original' | 'extension';

export interface InterestEarnedRow {
  loanId: string;
  loanNumber: string;
  customerId: string;
  customerName: string;
  loanDate: string;
  dueDate: string;
  principal: number;
  contractInterest: number;
  extensionInterest: number;
  earnedBeforePeriod: number;
  earnedThisPeriod: number;
  totalEarned: number;
  adjustment: number;
  finalEarned: number;
  status: LoanStatus;
  classification: LoanClassification;
}

export interface InterestEarnedFilters {
  fromDate: string;
  toDate: string;
  search?: string;
  status?: LoanStatus;
  classification?: LoanClassification;
  interestType?: InterestType;
}

export interface InterestEarnedTotals {
  principal: number;
  contractInterest: number;
  extensionInterest: number;
  earnedBeforePeriod: number;
  earnedThisPeriod: number;
  totalEarned: number;
  adjustment: number;
  finalEarned: number;
  count: number;
}

/**
 * The report's six summary cards. interestCollected/interestReceivable are
 * null (not estimated) — this app's Payment doesn't yet allocate a payment
 * between principal and interest, so there's no real, non-fabricated
 * "interest collected" figure to show yet.
 */
export interface InterestEarnedSummary {
  totalEarnedInterest: number;
  originalInterestEarned: number;
  extensionInterestEarned: number;
  interestAdjustments: number;
  interestCollected: number | null;
  interestReceivable: number | null;
}

export interface InterestEarnedOverview {
  summary: InterestEarnedSummary;
  totals: InterestEarnedTotals;
}

export interface InterestEarnedMonthlyPoint {
  month: string;
  currentYear: number;
  previousYear: number;
}

export interface InterestEarnedPeriod {
  label: string;
  periodStart: string;
  periodEndInclusive: string;
  termDays: number;
  contractAmount: number;
  dailyInterest: number;
  earnedDaysThisPeriod: number;
  earnedBeforePeriod: number;
  earnedThisPeriod: number;
  totalEarned: number;
}

export interface InterestEarnedLoanBreakdown {
  loanId: string;
  loanNumber: string;
  customerName: string;
  originalContractInterest: number;
  adjustedContractInterest: number;
  interestAdjustment: number;
  periods: InterestEarnedPeriod[];
}
