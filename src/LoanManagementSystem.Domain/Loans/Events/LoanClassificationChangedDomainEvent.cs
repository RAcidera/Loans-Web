using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when the lender manually changes a loan's classification. No
/// cash/ledger effect — handled by LoanClassificationChangedEventHandler,
/// which writes a LoanAuditLogEntry ("who changed what, when").
/// </summary>
public sealed record LoanClassificationChangedDomainEvent(
    LoanId LoanId, LoanClassification OldClassification, LoanClassification NewClassification, string ChangedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
