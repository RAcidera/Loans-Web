using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// The SRS's "Additional Recommendation: Loan Ledger" — a per-loan,
/// append-only financial history (Date, Transaction, Debit, Credit,
/// Balance) populated by domain-event handlers reacting to
/// LoanCreatedDomainEvent/PaymentRecordedDomainEvent/LoanExtendedDomainEvent,
/// the same event-driven pattern CashLedgerEntry already uses in this
/// codebase. Its own aggregate root, not a child of Loan, for the same
/// reason CashLedgerEntry is: an immutable fact about a transaction, never
/// edited in place — correcting a mistake means adding a new entry, not
/// rewriting history. RunningBalance is stamped at creation time from the
/// Loan's own Balance field (single source of truth for the number itself;
/// this ledger only records the history of how it got there).
/// </summary>
public class LoanLedgerEntry : AggregateRoot<LoanLedgerEntryId>
{
    public LoanId LoanId { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public LoanLedgerTransactionType TransactionType { get; private set; }

    /// <summary>Nullable PaymentId/LoanExtensionId — lets the frontend look up "this row's running balance" for a specific payment/extension without relying on row order.</summary>
    public string? ReferenceId { get; private set; }

    public Money Debit { get; private set; } = null!;
    public Money Credit { get; private set; } = null!;
    public Money RunningBalance { get; private set; } = null!;
    public string Remarks { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private LoanLedgerEntry() { } // EF Core

    private LoanLedgerEntry(LoanLedgerEntryId id, LoanId loanId, DateOnly transactionDate, LoanLedgerTransactionType type, string? referenceId, Money debit, Money credit, Money runningBalance, string remarks)
        : base(id)
    {
        LoanId = loanId;
        TransactionDate = transactionDate;
        TransactionType = type;
        ReferenceId = referenceId;
        Debit = debit;
        Credit = credit;
        RunningBalance = runningBalance;
        Remarks = remarks;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static LoanLedgerEntry Record(
        LoanId loanId, LoanLedgerTransactionType type, Money debit, Money credit, Money runningBalance,
        string remarks, DateOnly transactionDate, string? referenceId = null) =>
        new(LoanLedgerEntryId.New(), loanId, transactionDate, type, referenceId, debit, credit, runningBalance, remarks);

    /// <summary>
    /// Revises this row's Credit/TransactionDate/RunningBalance in place
    /// when the Payment it mirrors (ReferenceId == PaymentId) is edited —
    /// the same narrow exception to "ledgers are append-only" that
    /// CashLedgerEntry.ReviseForPaymentEdit documents; see that method.
    /// </summary>
    public void ReviseForPaymentEdit(Money credit, Money runningBalance, DateOnly transactionDate)
    {
        Credit = credit;
        RunningBalance = runningBalance;
        TransactionDate = transactionDate;
    }

    /// <summary>
    /// Shifts this row's stamped RunningBalance by a signed amount — used
    /// by PaymentDeletedEventHandler/LoanExtensionDeletedEventHandler to
    /// bring every row AFTER a deleted payment/extension back into
    /// agreement with the loan's real, post-deletion Balance. RunningBalance
    /// is a snapshot of the loan's Balance at the moment each row was
    /// recorded (see this class's doc comment); removing an earlier row
    /// changes what that trajectory should have been for every later one,
    /// so they must move too, not just the deleted row's own entry. Takes a
    /// plain decimal (not Money, which can't represent a negative delta)
    /// and clamps at zero, matching Money.Subtract's existing "overpayment
    /// clamps rather than throws" convention.
    /// </summary>
    public void ShiftRunningBalance(decimal delta)
    {
        RunningBalance = Money.Of(Math.Max(RunningBalance.Amount + delta, 0));
    }
}
