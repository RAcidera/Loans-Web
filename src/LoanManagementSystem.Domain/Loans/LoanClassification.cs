namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// User-managed — the lender's own manual risk judgment on a loan,
/// set via <see cref="Loan.ChangeClassification"/>. Deliberately a
/// separate field from <see cref="LoanStatus"/> rather than a single
/// "bad loan" checkbox: a loan can be Overdue (system) *and* Bad Loan
/// (lender's call) at the same time, and each needs to change
/// independently of the other.
/// </summary>
public enum LoanClassification
{
    Normal,
    WatchList,
    BadLoan,
}
