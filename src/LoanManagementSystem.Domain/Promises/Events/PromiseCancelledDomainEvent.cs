using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Promises.Events;

public sealed record PromiseCancelledDomainEvent(PromiseToPayId PromiseId, string PerformedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
