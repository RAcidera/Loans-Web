namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// System-managed — automatically determined by <see cref="Loan.RefreshOverdueStatus"/>
/// (or <see cref="Loan.RecordPayment"/>/<see cref="Loan.WriteOff"/> for Paid/WrittenOff),
/// never set directly by a user. Contrast with <see cref="LoanClassification"/>, which is
/// the lender's own risk judgment and never auto-derived.
/// </summary>
public enum LoanStatus
{
    Active,
    Extended,
    Paid,
    Overdue,

    /// <summary>
    /// Removed from active collection. Not auto-derivable from any other
    /// field (unlike Overdue/Paid) — set only via the explicit
    /// <see cref="Loan.WriteOff"/> action.
    /// </summary>
    WrittenOff,
}
