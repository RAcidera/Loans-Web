using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary;

/// <summary>
/// Backs the Diary Entry Detail "Audit Information" section (requirements
/// §13, §24) — who changed what, and when. Its own aggregate root, populated
/// by domain-event handlers reacting to DiaryEntry's events, mirroring
/// LoanAuditLogEntry exactly (see that type's doc comment for the reasoning).
/// </summary>
public class DiaryAuditLogEntry : AggregateRoot<DiaryAuditLogEntryId>
{
    public DiaryEntryId DiaryEntryId { get; private set; }
    public DiaryAuditAction Action { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private DiaryAuditLogEntry() { } // EF Core

    private DiaryAuditLogEntry(DiaryAuditLogEntryId id, DiaryEntryId diaryEntryId, DiaryAuditAction action, string description, string performedBy)
        : base(id)
    {
        DiaryEntryId = diaryEntryId;
        Action = action;
        Description = description;
        PerformedBy = performedBy;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public static DiaryAuditLogEntry Record(DiaryEntryId diaryEntryId, DiaryAuditAction action, string description, string performedBy) =>
        new(DiaryAuditLogEntryId.New(), diaryEntryId, action, description, performedBy);
}
