namespace LoanManagementSystem.Domain.Common;

/// <summary>
/// Base class for aggregate roots: the only entities a repository ever
/// loads or saves directly, and the only place invariants for a
/// consistency boundary are enforced. Collects domain events raised during
/// a business operation; AppDbContext reads and clears this list around
/// SaveChangesAsync so events are published only after a successful commit.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
