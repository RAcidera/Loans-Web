using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan extension is deleted. Handled by
/// LoanExtensionDeletedEventHandler, which removes the mirroring
/// loan_ledger row (via ReferenceId == ExtensionId) — extensions never had
/// a cash_ledger entry to begin with (see LoanExtendedDomainEvent), so
/// unlike PaymentDeletedDomainEvent there is no cash-ledger side to clean
/// up here. ResultingBalance is carried for the same reason
/// LoanExtendedDomainEvent carries it — to shift every later loan_ledger
/// row's stamped RunningBalance back into agreement with the loan's real,
/// post-deletion Balance.
/// </summary>
public sealed record LoanExtensionDeletedDomainEvent(LoanId LoanId, LoanExtensionId ExtensionId, Money DeletedCharges, Money ResultingBalance) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
