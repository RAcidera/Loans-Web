using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.CashLedger;

/// <summary>
/// CashLedgerEntry is its own aggregate root, not a child of Loan — matching
/// the SRS's own framing of "0. Cash Ledger / Funds Tracking" as a distinct
/// concern from the Loans/Customers/Payments group. Each entry is an
/// immutable fact about a cash movement; there is generally no "edit a
/// ledger entry" operation, only "add a new one" (you correct mistakes with
/// a reversing entry, not by rewriting history) — except for the one
/// narrow case Revise() exists for. See that method's doc comment for why
/// this entity carries a deliberate exception to its own rule.
/// </summary>
public class CashLedgerEntry : AggregateRoot<CashLedgerEntryId>
{
    public DateOnly TransactionDate { get; private set; }
    public CashTransactionType TransactionType { get; private set; }
    public string? ReferenceId { get; private set; } // nullable loan_id, per SRS
    public Money Amount { get; private set; } = null!;
    public string Remarks { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Set only on `payment_received` entries created from a Payment —
    /// lets PaymentEditedEventHandler find this exact row again when that
    /// Payment is later edited, without relying on ReferenceId (which
    /// holds the loan number, not unique per payment on a multi-payment loan).
    /// </summary>
    public PaymentId? SourcePaymentId { get; private set; }

    /// <summary>Whether this entry counts as Cash In (Formula 1) rather than Cash Out (Formula 2) — stored, not derived, because Adjustment can go either way (see ResolveDirection).</summary>
    public bool IsCashIn { get; private set; }

    /// <summary>
    /// True for the system-generated types a user can never manually add,
    /// edit, or delete from this ledger — those are changed from the
    /// corresponding Loan/Payment record instead so the ledger can't get
    /// out of sync with the fact it mirrors. See EditManual and
    /// AddCashTransactionCommandHandler's manual-types allowlist.
    /// </summary>
    public bool IsAutomatic => TransactionType is CashTransactionType.LoanRelease or CashTransactionType.PaymentReceived;

    private static readonly Dictionary<CashTransactionType, bool> FixedDirection = new()
    {
        [CashTransactionType.PaymentReceived] = true,
        [CashTransactionType.OwnerDeposit] = true,
        [CashTransactionType.LoanRelease] = false,
        [CashTransactionType.OwnerWithdrawal] = false,
        [CashTransactionType.Expense] = false,
    };

    private CashLedgerEntry() { } // EF Core

    private CashLedgerEntry(CashLedgerEntryId id, DateOnly transactionDate, CashTransactionType type, string? referenceId, Money amount, string remarks, PaymentId? sourcePaymentId, bool isCashIn)
        : base(id)
    {
        if (amount.Amount <= 0)
            throw new DomainException("A cash ledger entry's amount must be greater than zero.");

        TransactionDate = transactionDate;
        TransactionType = type;
        ReferenceId = referenceId;
        Amount = amount;
        Remarks = remarks;
        SourcePaymentId = sourcePaymentId;
        IsCashIn = isCashIn;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <param name="isCashIn">Required (and only meaningful) when <paramref name="type"/> is Adjustment — every other type's direction is fixed, see FixedDirection.</param>
    public static CashLedgerEntry Record(CashTransactionType type, Money amount, string remarks, DateOnly transactionDate, string? referenceId = null, PaymentId? sourcePaymentId = null, bool? isCashIn = null)
    {
        return new CashLedgerEntry(CashLedgerEntryId.New(), transactionDate, type, referenceId, amount, remarks, sourcePaymentId, ResolveDirection(type, isCashIn));
    }

    private static bool ResolveDirection(CashTransactionType type, bool? isCashIn)
    {
        if (type == CashTransactionType.Adjustment)
        {
            return isCashIn ?? throw new DomainException("An adjustment must specify whether it increases or decreases cash on hand.");
        }

        return FixedDirection[type];
    }

    /// <summary>
    /// Revises this entry's amount/date in place when the Payment or loan
    /// origination it mirrors is edited — a deliberate, narrow exception to
    /// "ledgers are append-only, correct with a new entry." That rule is
    /// right for free-standing ledger corrections; this row isn't
    /// free-standing, it's a direct 1:1 mirror of one specific,
    /// still-existing Payment/loan-release fact, and mistyped dates from
    /// field staff are common enough that a plain correction — not a pair
    /// of reversing entries a non-technical user would have to interpret in
    /// the Cash Funds screen — is the right tradeoff. Only ever called on a
    /// `payment_received` entry (via SourcePaymentId) or the one
    /// `loan_release` entry for a loan (via ReferenceId == loan number).
    /// </summary>
    public void Revise(Money amount, DateOnly transactionDate)
    {
        if (amount.Amount <= 0)
            throw new DomainException("A cash ledger entry's amount must be greater than zero.");

        Amount = amount;
        TransactionDate = transactionDate;
    }

    /// <summary>
    /// Full edit of a manually-entered transaction (owner deposit/withdrawal,
    /// expense, adjustment) — the Cash Transactions grid's row-menu "Edit"
    /// action. Unlike Revise() (system-only, keeps an automatic entry in
    /// sync with the Payment/Loan it mirrors), this is user-driven, so it's
    /// only ever valid on a row that isn't automatic — enforced here too,
    /// not just by the UI hiding the menu, since this is a domain invariant
    /// worth protecting regardless of caller.
    /// </summary>
    public void EditManual(CashTransactionType type, Money amount, string remarks, DateOnly transactionDate, bool? isCashIn)
    {
        if (IsAutomatic)
            throw new DomainException("Automatically generated transactions can only be changed from the Loan or Payment record that created them.");

        if (type is CashTransactionType.LoanRelease or CashTransactionType.PaymentReceived)
            throw new DomainException("A manual transaction cannot be changed to an automatic type.");

        if (amount.Amount <= 0)
            throw new DomainException("A cash ledger entry's amount must be greater than zero.");

        TransactionType = type;
        Amount = amount;
        Remarks = remarks;
        TransactionDate = transactionDate;
        IsCashIn = ResolveDirection(type, isCashIn);
    }

    /// <summary>The signed effect on Cash on Hand: positive for cash in, negative for cash out.</summary>
    public decimal SignedAmount => IsCashIn ? Amount.Amount : -Amount.Amount;
}
