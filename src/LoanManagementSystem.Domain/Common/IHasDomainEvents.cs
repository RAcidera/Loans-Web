namespace LoanManagementSystem.Domain.Common;

/// <summary>
/// AggregateRoot&lt;LoanId&gt; and AggregateRoot&lt;CustomerId&gt; are
/// different closed generic types, so a plain `OfType&lt;AggregateRoot&lt;Guid&gt;&gt;()`
/// in AppDbContext would never match anything. This non-generic interface
/// is what lets AppDbContext.SaveChangesAsync find "any tracked aggregate
/// with pending domain events" without caring which strongly-typed id it uses.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
