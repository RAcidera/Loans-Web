using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// Child entity of the Loan aggregate — matches Payments (SRS 3.4). Like
/// LoanExtension, only ever created through Loan.RecordPayment(), which is
/// what keeps the parent Loan's TotalPaid/Balance/Status in sync with it.
/// </summary>
public class Payment : Entity<PaymentId>
{
    public LoanId LoanId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public Money AmountPaid { get; private set; } = null!;
    public PaymentMethod PaymentMethod { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    /// <summary>Spec's Payment field (e.g. a GCash/bank transfer reference) — optional, no format enforced.</summary>
    public string? ReferenceNumber { get; private set; }

    private Payment() { } // EF Core

    internal Payment(LoanId loanId, DateOnly paymentDate, Money amountPaid, PaymentMethod paymentMethod, string notes, string? referenceNumber = null)
        : base(PaymentId.New())
    {
        if (amountPaid.Amount <= 0)
            throw new DomainException("A payment amount must be greater than zero.");

        LoanId = loanId;
        PaymentDate = paymentDate;
        AmountPaid = amountPaid;
        PaymentMethod = paymentMethod;
        Notes = notes;
        ReferenceNumber = referenceNumber;
    }

    /// <summary>Mutated only through Loan.EditPayment(), which also rolls the amount change into TotalPaid/Balance.</summary>
    internal void Edit(Money amountPaid, PaymentMethod paymentMethod, string notes, string? referenceNumber, DateOnly paymentDate)
    {
        if (amountPaid.Amount <= 0)
            throw new DomainException("A payment amount must be greater than zero.");

        AmountPaid = amountPaid;
        PaymentMethod = paymentMethod;
        Notes = notes;
        ReferenceNumber = referenceNumber;
        PaymentDate = paymentDate;
    }
}
