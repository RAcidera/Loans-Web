using MediatR;

namespace LoanManagementSystem.Domain.Common;

/// <summary>
/// A domain event: something that happened inside an aggregate that other
/// parts of the system (other aggregates, read models, integrations) may
/// care about. Extends MediatR's INotification so the same message can be
/// dispatched via IMediator.Publish() from the infrastructure layer after
/// a successful SaveChanges — see AppDbContext.SaveChangesAsync.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}
