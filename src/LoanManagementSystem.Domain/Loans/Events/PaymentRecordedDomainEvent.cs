using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a payment is recorded on a loan. Handled by
/// PaymentRecordedEventHandler, which creates the `payment_received` cash
/// ledger entry the SRS requires ("Each payment automatically creates a
/// payment_received entry in the Cash_Ledger table") — the exact
/// cross-aggregate consistency problem flagged as unresolved in the
/// Angular mock repository is solved here via a domain event instead of a
/// direct repository-to-repository call. Also handled by the same-named
/// handler to write this payment's LoanLedgerEntry row — ResultingBalance
/// is carried explicitly so that row's RunningBalance reflects the Loan's
/// Balance right after this payment, without re-fetching the aggregate.
/// </summary>
public sealed record PaymentRecordedDomainEvent(LoanId LoanId, PaymentId PaymentId, Money AmountPaid, DateOnly PaymentDate, Money ResultingBalance) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
