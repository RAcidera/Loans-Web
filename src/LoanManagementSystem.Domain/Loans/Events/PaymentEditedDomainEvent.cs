using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when an existing payment is edited (amount and/or date). Handled
/// by PaymentEditedEventHandler, which revises the matching cash_ledger
/// `payment_received` entry and loan_ledger row in place — a deliberate,
/// narrow exception to this codebase's general "ledgers are append-only,
/// correct with a new entry, not an edit" rule (see CashLedgerEntry's own
/// doc comment). That rule holds for free-standing ledger corrections; this
/// case is different because the ledger row is a direct 1:1 mirror of one
/// specific, still-existing Payment record, and mistyped payment dates
/// from field staff are common enough that a straightforward correction —
/// not a pair of reversing entries a non-technical user would need to
/// interpret — is the right tradeoff here.
/// </summary>
public sealed record PaymentEditedDomainEvent(LoanId LoanId, PaymentId PaymentId, Money NewAmountPaid, DateOnly NewPaymentDate, Money ResultingBalance) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
