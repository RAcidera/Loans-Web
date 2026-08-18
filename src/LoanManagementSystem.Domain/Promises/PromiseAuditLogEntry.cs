using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Promises;

/// <summary>Requirements §24's promise audit trail — mirrors LoanAuditLogEntry/DiaryAuditLogEntry exactly.</summary>
public class PromiseAuditLogEntry : AggregateRoot<PromiseAuditLogEntryId>
{
    public PromiseToPayId PromiseId { get; private set; }
    public PromiseAuditAction Action { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private PromiseAuditLogEntry() { } // EF Core

    private PromiseAuditLogEntry(PromiseAuditLogEntryId id, PromiseToPayId promiseId, PromiseAuditAction action, string description, string performedBy)
        : base(id)
    {
        PromiseId = promiseId;
        Action = action;
        Description = description;
        PerformedBy = performedBy;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public static PromiseAuditLogEntry Record(PromiseToPayId promiseId, PromiseAuditAction action, string description, string performedBy) =>
        new(PromiseAuditLogEntryId.New(), promiseId, action, description, performedBy);
}
