// Domain layer — mirrors the Loan_Extensions table in the SRS.

export interface LoanExtension {
  extensionId: string;
  loanId: string;
  extensionDate: string;
  extensionDays: number;
  /** The fee added for extending — see the Loan entity's Outstanding Balance formula. */
  additionalChargesAmount: number;
  remarks: string;
}
