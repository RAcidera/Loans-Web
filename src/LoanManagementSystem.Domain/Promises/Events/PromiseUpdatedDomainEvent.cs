using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Promises.Events;

public sealed record PromiseUpdatedDomainEvent(PromiseToPayId PromiseId, string EditedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
