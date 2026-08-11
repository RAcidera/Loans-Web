using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// Backs the Loan Details "Audit Log" tab — who changed what, and when,
/// for the loan's own fields/classification/write-off (financial
/// movements go to LoanLedgerEntry instead). Its own aggregate root for
/// the same reason LoanLedgerEntry is: an immutable fact, populated by
/// domain-event handlers reacting to LoanEditedDomainEvent/
/// LoanClassificationChangedDomainEvent/LoanWrittenOffDomainEvent.
/// </summary>
public class LoanAuditLogEntry : AggregateRoot<LoanAuditLogEntryId>
{
    public LoanId LoanId { get; private set; }
    public LoanAuditAction Action { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private LoanAuditLogEntry() { } // EF Core

    private LoanAuditLogEntry(LoanAuditLogEntryId id, LoanId loanId, LoanAuditAction action, string description, string performedBy)
        : base(id)
    {
        LoanId = loanId;
        Action = action;
        Description = description;
        PerformedBy = performedBy;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public static LoanAuditLogEntry Record(LoanId loanId, LoanAuditAction action, string description, string performedBy) =>
        new(LoanAuditLogEntryId.New(), loanId, action, description, performedBy);
}
