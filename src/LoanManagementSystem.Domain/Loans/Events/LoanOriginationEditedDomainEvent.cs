using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when EditLoan changes Principal and/or StartDate — handled by
/// LoanOriginationEditedEventHandler, which revises the mirrored
/// `loan_release` entry in cash_ledger (found by ReferenceId == loan
/// number, unique per loan since a loan only ever gets one release entry)
/// so a corrected loan date or principal doesn't leave the cash ledger
/// showing the old, wrong figures — the same reasoning as
/// PaymentEditedDomainEvent for payments.
///
/// Deliberately scoped to cash_ledger only, not this loan's own
/// LoanLedgerEntry history: the loan's "Loan Released"/"Interest Added"
/// rows carry a RunningBalance that every later entry in the chain was
/// computed relative to, so revising them in place could silently
/// desynchronize every entry after them. cash_ledger's `payment_received`/
/// `loan_release` rows don't chain running balances the same way, so
/// revising just this one row in place is safe.
/// </summary>
public sealed record LoanOriginationEditedDomainEvent(LoanId LoanId, Money NewPrincipal, DateOnly NewStartDate) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
