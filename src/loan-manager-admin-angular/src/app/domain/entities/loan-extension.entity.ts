// Domain layer — mirrors the Loan_Extensions table in the SRS.

export interface LoanExtension {
  extensionId: string;
  loanId: string;
  extensionDate: string;
  extensionDays: number;
  additionalInterestAmount: number;
  /** A separate fee from additionalInterestAmount — see the Loan entity's Outstanding Balance formula. */
  additionalChargesAmount: number;
  remarks: string;
}
