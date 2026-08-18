using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Promises.Events;

public sealed record PromiseRescheduledDomainEvent(PromiseToPayId PromiseId, DateOnly NewPromiseDate, string PerformedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
