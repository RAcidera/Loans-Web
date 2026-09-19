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
/// this ledger only records the history of how it got there) — but that
/// makes it a snapshot of recording order, not transaction-date order, so
/// it's only correct when entries happen to be recorded in date order. An
/// antedated/backdated entry breaks that assumption; readers that must be
/// correct even then (GenerateLoanSoaQuery, GetLoanLedgerQuery) recompute
/// a true running balance from SignedAmount over the full ledger sorted by
/// TransactionDate instead of trusting this stamped value.
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

    /// <summary>Debit minus Credit — this entry's own effect on the balance, independent of when it was recorded. Used to recompute a true chronological running balance at read time (see GenerateLoanSoaQuery/GetLoanLedgerQuery), since the stamped RunningBalance below is only correct when entries are recorded in date order.</summary>
    public decimal SignedAmount => Debit.Amount - Credit.Amount;

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
    /// Revises this row's Credit/TransactionDate/RunningBalance/Remarks in
    /// place when the Payment it mirrors (ReferenceId == PaymentId) is
    /// edited — the same narrow exception to "ledgers are append-only" that
    /// CashLedgerEntry.ReviseForPaymentEdit documents; see that method.
    /// Remarks is included so an edited payment's notes stay in sync with
    /// what Statement of Account V2 displays for this row.
    /// </summary>
    public void ReviseForPaymentEdit(Money credit, Money runningBalance, DateOnly transactionDate, string remarks)
    {
        Credit = credit;
        RunningBalance = runningBalance;
        TransactionDate = transactionDate;
        Remarks = remarks;
    }

    /// <summary>
    /// Revises this row's Debit/TransactionDate in place — used by both
    /// LoanOriginationEditedEventHandler (keeping the LoanReleased/
    /// InterestAdded rows a loan was opened with in sync with a corrected
    /// principal, disbursement date, or interest amount) and
    /// LoanExtensionEditedEventHandler (keeping an Extension row in sync
    /// with a corrected charge amount or date). Safe to do (unlike once
    /// revising any of these rows in place would have been) precisely
    /// because readers no longer trust the stamped RunningBalance chain —
    /// see this class's doc comment — so there's nothing left to
    /// desynchronize by moving a row's own date/amount.
    /// </summary>
    public void ReviseDebit(Money debit, DateOnly transactionDate)
    {
        Debit = debit;
        TransactionDate = transactionDate;
    }

    /// <summary>
    /// Revises this Extension row's Debit/TransactionDate/Remarks in place
    /// when the LoanExtension it mirrors is edited — a sibling of
    /// ReviseDebit kept as its own method (rather than adding a Remarks
    /// parameter to ReviseDebit) because ReviseDebit is also shared by
    /// LoanOriginationEditedEventHandler for the LoanReleased/InterestAdded
    /// rows, which have no borrower-facing remarks to keep in sync.
    /// </summary>
    public void ReviseExtension(Money debit, DateOnly transactionDate, string remarks)
    {
        Debit = debit;
        TransactionDate = transactionDate;
        Remarks = remarks;
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
