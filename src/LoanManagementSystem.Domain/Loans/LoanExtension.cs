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
    public Money AdditionalInterestAmount { get; private set; } = null!;

    /// <summary>
    /// A separate fee from AdditionalInterestAmount — e.g. a flat
    /// processing charge for granting the extension, as distinct from
    /// interest accruing on the extended term. Kept as its own line item
    /// because the loan's "Extension Charges" total (used in the
    /// Outstanding Balance formula) sums this field, not interest.
    /// </summary>
    public Money AdditionalChargesAmount { get; private set; } = null!;

    public string Remarks { get; private set; } = string.Empty;

    private LoanExtension() { } // EF Core

    internal LoanExtension(LoanId loanId, DateOnly extensionDate, int extensionDays, Money additionalInterestAmount, Money additionalChargesAmount, string remarks)
        : base(LoanExtensionId.New())
    {
        if (extensionDays <= 0)
            throw new DomainException("An extension must add at least one day.");

        LoanId = loanId;
        ExtensionDate = extensionDate;
        ExtensionDays = extensionDays;
        AdditionalInterestAmount = additionalInterestAmount;
        AdditionalChargesAmount = additionalChargesAmount;
        Remarks = remarks;
    }

    /// <summary>
    /// Mutated only through Loan.EditExtension(), which rolls this
    /// extension's old ExtensionDays/AdditionalInterestAmount/
    /// AdditionalChargesAmount contribution out of DueDate/TotalInterest/
    /// TotalExtensionCharges before applying the new values here.
    /// </summary>
    internal void Edit(int extensionDays, Money additionalInterestAmount, Money additionalChargesAmount, string remarks, DateOnly extensionDate)
    {
        if (extensionDays <= 0)
            throw new DomainException("An extension must add at least one day.");

        ExtensionDate = extensionDate;
        ExtensionDays = extensionDays;
        AdditionalInterestAmount = additionalInterestAmount;
        AdditionalChargesAmount = additionalChargesAmount;
        Remarks = remarks;
    }
}
