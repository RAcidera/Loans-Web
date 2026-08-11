using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans.Events;

/// <summary>
/// Raised when a loan is disbursed. The Application layer's
/// LoanCreatedEventHandler reacts to this by creating a `loan_release`
/// entry in the cash ledger (SRS: "Transaction Type Effects: loan_release
/// → Cash Out") — this is how the Loans and CashLedger boundaries stay
/// consistent without LoanRepository depending on CashLedgerRepository.
///
/// StartDate is carried explicitly rather than relying on OccurredOnUtc:
/// OccurredOnUtc is when this event was raised in wall-clock time, which
/// is right for audit logging but wrong for the ledger entry's own
/// transaction date when a loan is backdated (e.g. seeding historical
/// data, or a delayed data-entry correction) — the cash movement happened
/// on the loan's StartDate, not necessarily "now".
///
/// Interest is carried too (not just Principal) so LoanCreatedEventHandler
/// can also write the "Loan Released" + "Interest Added" LoanLedgerEntry
/// rows the SRS's ledger example shows for origination, without a second
/// database round trip back to the Loan aggregate to read it.
/// </summary>
public sealed record LoanCreatedDomainEvent(LoanId LoanId, CustomerId CustomerId, Money Principal, Money Interest, DateOnly StartDate) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
