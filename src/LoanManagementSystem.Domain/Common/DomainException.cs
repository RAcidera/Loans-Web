namespace LoanManagementSystem.Domain.Common;

/// <summary>
/// Thrown when an operation would violate a domain invariant (e.g.
/// recording a payment larger than the outstanding balance). Distinct from
/// framework/infrastructure exceptions so the API layer can map it to a
/// 400 Bad Request rather than a 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
