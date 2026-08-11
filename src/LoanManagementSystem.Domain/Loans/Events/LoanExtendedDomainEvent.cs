using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan is extended. Unlike LoanCreated/PaymentRecorded,
/// this does NOT move cash — an extension adds interest/fee to what's
/// owed, it doesn't disburse or collect anything — so per the SRS's own
/// cash-flow rules, no cash ledger entry follows from this event. It DOES
/// get a LoanLedgerEntry row though (the SRS's own ledger example shows an
/// "Extension" line) — LoanExtendedEventHandler combines
/// AdditionalInterestAmount + AdditionalChargesAmount into one Debit row;
/// ResultingBalance is carried explicitly for the same reason
/// PaymentRecordedDomainEvent carries it.
/// </summary>
public sealed record LoanExtendedDomainEvent(LoanId LoanId, LoanExtensionId ExtensionId, DateOnly ExtensionDate, int ExtensionDays, Money AdditionalInterestAmount, Money AdditionalChargesAmount, Money ResultingBalance) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
