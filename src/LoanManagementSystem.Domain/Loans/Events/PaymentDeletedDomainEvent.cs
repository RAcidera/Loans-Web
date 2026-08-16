using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a payment is deleted. Handled by PaymentDeletedEventHandler,
/// which removes the mirroring cash_ledger `payment_received` entry (via
/// SourcePaymentId) and loan_ledger row (via ReferenceId == PaymentId) —
/// without this, both ledgers keep a row for a payment that no longer
/// exists, and Cash on Hand stays overstated by the deleted amount
/// indefinitely. ResultingBalance is carried for the same reason
/// PaymentEditedDomainEvent carries it — to shift every later loan_ledger
/// row's stamped RunningBalance back into agreement with the loan's real,
/// post-deletion Balance.
/// </summary>
public sealed record PaymentDeletedDomainEvent(LoanId LoanId, PaymentId PaymentId, Money DeletedAmount, Money ResultingBalance) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
