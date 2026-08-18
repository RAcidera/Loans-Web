using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary.Events;

/// <summary>Raised when a diary entry is created. Handled by DiaryEntryCreatedEventHandler, which writes a DiaryAuditLogEntry.</summary>
public sealed record DiaryEntryCreatedDomainEvent(DiaryEntryId DiaryEntryId, string CreatedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
