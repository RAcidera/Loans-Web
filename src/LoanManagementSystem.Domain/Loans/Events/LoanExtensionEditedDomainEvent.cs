using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when EditExtension changes an extension's AdditionalChargesAmount
/// and/or ExtensionDate — handled by LoanExtensionEditedEventHandler, which
/// revises the mirroring loan_ledger row in place (via ReferenceId ==
/// ExtensionId) the same way LoanOriginationEditedEventHandler revises the
/// LoanReleased/InterestAdded rows: safe because SOA/ledger readers
/// recompute a chronological running balance instead of trusting the
/// stamped one (see LoanLedgerEntry's doc comment). Without this, an edited
/// extension's fee correction never reaches loan_ledger, so every payment
/// recorded after that extension keeps showing a running balance off by
/// (new charge - old charge) even though the loan's own Balance is correct.
/// Extensions never had a cash_ledger entry (see LoanExtendedDomainEvent),
/// so unlike PaymentEditedDomainEvent/LoanOriginationEditedDomainEvent there
/// is no cash-ledger side to revise here.
/// </summary>
public sealed record LoanExtensionEditedDomainEvent(LoanId LoanId, LoanExtensionId ExtensionId, Money NewAdditionalChargesAmount, DateOnly NewExtensionDate) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
