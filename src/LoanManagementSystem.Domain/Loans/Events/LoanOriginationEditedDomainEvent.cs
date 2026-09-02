using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when EditLoan changes Principal, StartDate, and/or the resulting
/// TotalInterest (a rate/interest-amount-only edit counts too — NewPrincipal/
/// NewStartDate on this event are simply the loan's current values in that
/// case, so revising against them is a harmless no-op for the fields that
/// didn't actually change) — handled by LoanOriginationEditedEventHandler,
/// which revises the mirrored `loan_release` entry in cash_ledger (found by
/// ReferenceId == loan number, unique per loan since a loan only ever gets
/// one release entry) so a corrected loan date, principal, or interest
/// doesn't leave the cash ledger showing the old, wrong figures — the same
/// reasoning as PaymentEditedDomainEvent for payments. Also revises this
/// loan's own loan_ledger "Loan Released"/"Interest Added" rows in place
/// (Debit and TransactionDate), re-reading TotalInterest from the loan
/// itself since it isn't carried on this event — see LoanLedgerEntry's doc
/// comment for why revising those two rows is safe now that SOA/ledger
/// readers recompute a chronological running balance instead of trusting
/// the stamped one, unlike when this event was first introduced.
/// </summary>
public sealed record LoanOriginationEditedDomainEvent(LoanId LoanId, Money NewPrincipal, DateOnly NewStartDate) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
