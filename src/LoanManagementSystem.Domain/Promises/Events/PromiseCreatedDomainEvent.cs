using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Promises.Events;

public sealed record PromiseCreatedDomainEvent(PromiseToPayId PromiseId, string CreatedBy) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
