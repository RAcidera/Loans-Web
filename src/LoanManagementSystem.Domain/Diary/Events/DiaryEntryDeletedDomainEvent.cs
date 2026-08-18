using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary.Events;

/// <summary>
/// Raised by DiaryEntry.MarkForDeletion() before the aggregate is removed
/// from the repository — EF's ChangeTracker still reports a Deleted-state
/// entity via ChangeTracker.Entries(), so AppDbContext.SaveChangesAsync
/// still collects and publishes this event even though the row is gone
/// after the commit (same trick LoanExtensionDeletedDomainEvent relies on).
/// </summary>
public sealed record DiaryEntryDeletedDomainEvent(DiaryEntryId DiaryEntryId, string DeletedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
