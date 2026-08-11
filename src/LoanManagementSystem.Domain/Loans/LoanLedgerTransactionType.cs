namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// The "Additional Recommendation: Loan Ledger" transaction types — every
/// financial movement on a loan (never a classification/write-off/edit,
/// which have no cash effect and go to LoanAuditLogEntry instead).
/// </summary>
public enum LoanLedgerTransactionType
{
    LoanReleased,
    InterestAdded,
    Payment,
    Extension,
}
