using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan's own fields (Loan Date, Due Date, Interest Rate,
/// Interest Amount, Remarks) are edited post-creation — e.g. a goodwill
/// discount before an early payoff. Handled by LoanEditedEventHandler,
/// which writes a LoanAuditLogEntry.
/// </summary>
public sealed record LoanEditedDomainEvent(LoanId LoanId, string EditedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
