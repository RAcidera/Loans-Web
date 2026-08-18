using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary.Events;

/// <summary>Raised once, when a financial snapshot is attached to a diary entry at creation — never raised again, since the snapshot can't be recaptured (requirements §10).</summary>
public sealed record FinancialSnapshotCapturedDomainEvent(DiaryEntryId DiaryEntryId, string CapturedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
