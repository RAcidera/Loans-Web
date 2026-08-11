using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.CashLedger;

/// <summary>
/// CashLedgerEntry is its own aggregate root, not a child of Loan — matching
/// the SRS's own framing of "0. Cash Ledger / Funds Tracking" as a distinct
/// concern from the Loans/Customers/Payments group. Each entry is an
/// immutable fact about a cash movement; there is no "edit a ledger entry"
/// operation, only "add a new one," which is the correct model for a ledger
/// (you correct mistakes with a reversing entry, not by rewriting history).
/// </summary>
public class CashLedgerEntry : AggregateRoot<CashLedgerEntryId>
{
    public DateOnly TransactionDate { get; private set; }
    public CashTransactionType TransactionType { get; private set; }
    public string? ReferenceId { get; private set; } // nullable loan_id, per SRS
    public Money Amount { get; private set; } = null!;
    public string Remarks { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private static readonly HashSet<CashTransactionType> CashInTypes = new()
    {
        CashTransactionType.PaymentReceived,
        CashTransactionType.OwnerDeposit,
    };

    private CashLedgerEntry() { } // EF Core

    private CashLedgerEntry(CashLedgerEntryId id, DateOnly transactionDate, CashTransactionType type, string? referenceId, Money amount, string remarks)
        : base(id)
    {
        if (amount.Amount <= 0)
            throw new DomainException("A cash ledger entry's amount must be greater than zero.");

        TransactionDate = transactionDate;
        TransactionType = type;
        ReferenceId = referenceId;
        Amount = amount;
        Remarks = remarks;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static CashLedgerEntry Record(CashTransactionType type, Money amount, string remarks, DateOnly transactionDate, string? referenceId = null)
    {
        return new CashLedgerEntry(CashLedgerEntryId.New(), transactionDate, type, referenceId, amount, remarks);
    }

    /// <summary>Whether this entry counts as Cash In (Formula 1) rather than Cash Out (Formula 2).</summary>
    public bool IsCashIn => CashInTypes.Contains(TransactionType);

    /// <summary>The signed effect on Cash on Hand: positive for cash in, negative for cash out.</summary>
    public decimal SignedAmount => IsCashIn ? Amount.Amount : -Amount.Amount;
}
