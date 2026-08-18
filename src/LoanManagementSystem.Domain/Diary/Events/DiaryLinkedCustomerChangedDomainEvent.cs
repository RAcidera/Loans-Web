using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary.Events;

/// <summary>Raised alongside DiaryEntryUpdatedDomainEvent only when CustomerId actually changed — requirements §24 tracks this as its own audit action.</summary>
public sealed record DiaryLinkedCustomerChangedDomainEvent(DiaryEntryId DiaryEntryId, string ChangedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
