using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// Child entity of the Loan aggregate — matches Loan_Extensions (SRS 3.3).
/// Has identity (needed for the history list and EF Core's own tracking)
/// but is never loaded, saved, or reasoned about independently of its
/// parent Loan; all mutation happens through Loan.Extend().
/// </summary>
public class LoanExtension : Entity<LoanExtensionId>
{
    public LoanId LoanId { get; private set; }
    public DateOnly ExtensionDate { get; private set; }
    public int ExtensionDays { get; private set; }

    /// <summary>
    /// The fee for granting the extension — e.g. a flat processing charge.
    /// Kept as its own line item because the loan's "Extension Charges"
    /// total (used in the Outstanding Balance formula) sums this field.
    /// Extensions no longer add interest of their own (that field was
    /// removed — "Additional Charges" alone covers the extension fee).
    /// </summary>
    public Money AdditionalChargesAmount { get; private set; } = null!;

    public string Remarks { get; private set; } = string.Empty;

    /// <summary>
    /// Insertion-order tiebreaker. ExtensionDate alone can't disambiguate
    /// two extensions granted on the same calendar day (a real possibility —
    /// e.g. correcting a mistaken extension immediately), and a GUID
    /// primary key gives no reliable fallback ordering on its own (SQL
    /// Server returns rows in clustered-index/physical order on a scan,
    /// which for a random GUID key has no relationship to insertion order).
    /// Every read site orders by (ExtensionDate, CreatedAtUtc) together.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    private LoanExtension() { } // EF Core

    internal LoanExtension(LoanId loanId, DateOnly extensionDate, int extensionDays, Money additionalChargesAmount, string remarks)
        : base(LoanExtensionId.New())
    {
        if (extensionDays <= 0)
            throw new DomainException("An extension must add at least one day.");

        LoanId = loanId;
        ExtensionDate = extensionDate;
        ExtensionDays = extensionDays;
        AdditionalChargesAmount = additionalChargesAmount;
        Remarks = remarks;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Mutated only through Loan.EditExtension(), which rolls this
    /// extension's old ExtensionDays/AdditionalChargesAmount contribution
    /// out of DueDate/TotalExtensionCharges before applying the new values here.
    /// </summary>
    internal void Edit(int extensionDays, Money additionalChargesAmount, string remarks, DateOnly extensionDate)
    {
        if (extensionDays <= 0)
            throw new DomainException("An extension must add at least one day.");

        ExtensionDate = extensionDate;
        ExtensionDays = extensionDays;
        AdditionalChargesAmount = additionalChargesAmount;
        Remarks = remarks;
    }
}
