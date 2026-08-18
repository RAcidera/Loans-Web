using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary.Events;

/// <summary>Raised whenever a diary entry's title/notes/category/date/time is edited — never raised for a snapshot change, since the snapshot is immutable (requirements §10).</summary>
public sealed record DiaryEntryUpdatedDomainEvent(DiaryEntryId DiaryEntryId, string EditedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
