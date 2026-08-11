using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan is written off (removed from active collection).
/// Handled by LoanWrittenOffEventHandler, which writes a LoanAuditLogEntry.
/// </summary>
public sealed record LoanWrittenOffDomainEvent(LoanId LoanId, string WrittenOffBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
