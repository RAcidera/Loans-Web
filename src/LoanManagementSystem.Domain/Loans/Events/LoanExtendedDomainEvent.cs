using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan is extended. Unlike LoanCreated/PaymentRecorded,
/// this does NOT move cash — an extension adds a fee to what's owed, it
/// doesn't disburse or collect anything — so per the SRS's own cash-flow
/// rules, no cash ledger entry follows from this event. It DOES get a
/// LoanLedgerEntry row though (the SRS's own ledger example shows an
/// "Extension" line) — LoanExtendedEventHandler records AdditionalChargesAmount
/// as that row's Debit; ResultingBalance is carried explicitly for the same
/// reason PaymentRecordedDomainEvent carries it. Remarks is the lender's
/// borrower-facing note for this extension (e.g. "Additional interest Aug
/// 2-Sep 2") — written into that same ledger row's Remarks, for the same
/// reason PaymentRecordedDomainEvent carries Notes.
/// </summary>
public sealed record LoanExtendedDomainEvent(LoanId LoanId, LoanExtensionId ExtensionId, DateOnly ExtensionDate, int ExtensionDays, Money AdditionalChargesAmount, Money ResultingBalance, string Remarks) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
