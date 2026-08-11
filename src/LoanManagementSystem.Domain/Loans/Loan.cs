using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// Loan aggregate root. Owns LoanExtension and Payment as child entities —
/// every mutation to the loan's balance, due date, or status goes through
/// a method here, which is what keeps those fields from ever being set
/// inconsistently with the extension/payment history that justifies them.
/// </summary>
public class Loan : AggregateRoot<LoanId>
{
    private readonly List<LoanExtension> _extensions = new();
    private readonly List<Payment> _payments = new();
    private readonly List<LoanDocument> _documents = new();

    public CustomerId CustomerId { get; private set; }
    public Money PrincipalAmount { get; private set; } = null!;
    public InterestRate InterestRate { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Money TotalInterest { get; private set; } = null!;

    /// <summary>
    /// Sum of every extension's AdditionalChargesAmount — kept separate
    /// from TotalInterest so the Outstanding Balance formula's three
    /// components (Principal + Interest + Extension Charges) can be shown
    /// as distinct line items, not folded into one interest figure.
    /// </summary>
    public Money TotalExtensionCharges { get; private set; } = null!;

    public Money TotalAmountDue { get; private set; } = null!;
    public Money TotalPaid { get; private set; } = null!;
    public Money Balance { get; private set; } = null!;
    public LoanStatus Status { get; private set; }

    /// <summary>
    /// The lender's manual risk judgment — see LoanClassification. Defaults
    /// to Normal at origination and only ever changes via ChangeClassification.
    /// </summary>
    public LoanClassification Classification { get; private set; }

    /// <summary>Free-text notes on the loan — spec's Loan field, editable via EditLoan() after creation.</summary>
    public string Remarks { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// A stable, human-friendly sequential number (displayed as "LM-001",
    /// "LM-002", ...) — a friendlier stand-in for the internal GUID id
    /// wherever loans are shown in the UI. Assigned by the database
    /// (AUTO_INCREMENT, see LoanConfiguration) on insert, not here — it's
    /// 0 on a freshly originated loan until SaveChanges populates it.
    /// </summary>
    public int LoanNumber { get; private set; }

    public IReadOnlyCollection<LoanExtension> Extensions => _extensions.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
    public IReadOnlyCollection<LoanDocument> Documents => _documents.AsReadOnly();

    private Loan() { } // EF Core

    private Loan(LoanId id, CustomerId customerId, Money principal, InterestRate rate, DateOnly startDate, int termDays)
        : base(id)
    {
        CustomerId = customerId;
        PrincipalAmount = principal;
        InterestRate = rate;
        StartDate = startDate;
        DueDate = startDate.AddDays(termDays);
        TotalInterest = rate.CalculateInterest(principal);
        TotalExtensionCharges = Money.Zero;
        TotalAmountDue = principal.Add(TotalInterest);
        TotalPaid = Money.Zero;
        Balance = TotalAmountDue;
        Status = LoanStatus.Active;
        Classification = LoanClassification.Normal;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Originates a new loan (SRS 3.2). termDays defaults to 60 and rate to
    /// 3% flat, matching the SRS's stated defaults, but both are accepted
    /// as parameters so a future change in business terms doesn't require
    /// touching this method's signature.
    /// </summary>
    public static Loan Originate(CustomerId customerId, Money principal, InterestRate rate, DateOnly startDate, int termDays = 60)
    {
        if (principal.Amount <= 0)
            throw new DomainException("A loan's principal must be greater than zero.");
        if (termDays <= 0)
            throw new DomainException("A loan's term must be at least one day.");

        var loan = new Loan(LoanId.New(), customerId, principal, rate, startDate, termDays);
        loan.RaiseDomainEvent(new LoanCreatedDomainEvent(loan.Id, customerId, principal, loan.TotalInterest, startDate));
        return loan;
    }

    /// <summary>
    /// Extends the due date and adds a fee (SRS 3.3). Does not touch cash —
    /// see LoanExtendedDomainEvent for why.
    /// </summary>
    public LoanExtension Extend(int extensionDays, Money additionalInterestAmount, Money additionalChargesAmount, string remarks, DateOnly extensionDate)
    {
        EnsureNotPaid();

        var extension = new LoanExtension(Id, extensionDate, extensionDays, additionalInterestAmount, additionalChargesAmount, remarks);
        _extensions.Add(extension);

        DueDate = DueDate.AddDays(extensionDays);
        TotalInterest = TotalInterest.Add(additionalInterestAmount);
        TotalExtensionCharges = TotalExtensionCharges.Add(additionalChargesAmount);
        TotalAmountDue = TotalAmountDue.Add(additionalInterestAmount).Add(additionalChargesAmount);
        Balance = TotalAmountDue.Subtract(TotalPaid);
        Status = LoanStatus.Extended;

        RaiseDomainEvent(new LoanExtendedDomainEvent(Id, extension.Id, extensionDate, extensionDays, additionalInterestAmount, additionalChargesAmount, Balance));
        return extension;
    }

    /// <summary>
    /// The lender's manual risk judgment (spec: "Change Classification"
    /// button on the Loan Details page) — independent of Status, so a loan
    /// can be Overdue (system) and BadLoan (lender's call) simultaneously.
    /// </summary>
    public void ChangeClassification(LoanClassification classification, string changedBy)
    {
        if (Classification == classification) return;

        var previous = Classification;
        Classification = classification;
        RaiseDomainEvent(new LoanClassificationChangedDomainEvent(Id, previous, classification, changedBy));
    }

    /// <summary>
    /// Removes the loan from active collection. Unlike Overdue/Paid, this
    /// is never auto-derived — it's an explicit lender decision (spec:
    /// Loan Status "Written Off — Removed from active collection").
    /// </summary>
    public void WriteOff(string writtenOffBy)
    {
        if (Status == LoanStatus.WrittenOff) return;
        if (Status == LoanStatus.Paid)
            throw new DomainException("A fully paid loan cannot be written off.");

        Status = LoanStatus.WrittenOff;
        RaiseDomainEvent(new LoanWrittenOffDomainEvent(Id, writtenOffBy));
    }

    /// <summary>
    /// Overrides Loan Date/Due Date/Interest Rate/Interest Amount/Remarks
    /// after origination (spec: "there are cases where customers pay early
    /// and the lender may provide a goodwill discount by reducing
    /// interest"). Each parameter is applied only if supplied, so a caller
    /// can change just one field.
    ///
    /// Interest Amount is a direct override, not recomputed from
    /// InterestRate * Principal — TotalInterest is a single running total
    /// that already folds in every extension's AdditionalInterestAmount, so
    /// treating it as "one number the lender can adjust" (same as at
    /// origination) is simpler than trying to split it back into an
    /// origination component and an extensions component. Editing
    /// InterestRate alone (no explicit InterestAmount) recomputes the
    /// origination-interest portion from the new rate, preserving whatever
    /// extension interest has already accrued.
    ///
    /// Allowed on a Paid loan (a correction after the fact should still be
    /// possible) — only WrittenOff blocks further edits, unlike Extend()/
    /// RecordPayment() which also block on Paid (starting a new financial
    /// event on an already-settled loan doesn't make sense; correcting an
    /// existing one does).
    /// </summary>
    public void EditLoan(DateOnly? startDate, DateOnly? dueDate, InterestRate? interestRate, Money? interestAmount, string? remarks, string editedBy)
    {
        EnsureNotWrittenOff();

        if (interestRate is { } newRate && interestAmount is null)
        {
            var extensionInterest = TotalInterest.Subtract(InterestRate.CalculateInterest(PrincipalAmount));
            TotalInterest = newRate.CalculateInterest(PrincipalAmount).Add(extensionInterest);
        }
        if (interestRate is { } rate) InterestRate = rate;
        if (interestAmount is { } newInterestAmount) TotalInterest = newInterestAmount;
        if (startDate is { } newStartDate) StartDate = newStartDate;
        if (dueDate is { } newDueDate) DueDate = newDueDate;
        if (remarks is not null) Remarks = remarks;

        if (DueDate < StartDate)
            throw new DomainException("A loan's due date cannot be before its start date.");

        TotalAmountDue = PrincipalAmount.Add(TotalInterest).Add(TotalExtensionCharges);
        Balance = TotalAmountDue.Subtract(TotalPaid);

        RaiseDomainEvent(new LoanEditedDomainEvent(Id, editedBy));
    }

    /// <summary>
    /// Edits an existing payment, rolling its old AmountPaid out of
    /// TotalPaid before applying the new one — same "not just append a
    /// correction" reasoning as EditExtension. Allowed regardless of
    /// current Status (including Paid — reducing a Paid loan's TotalPaid
    /// via a correction should un-pay it; see RefreshPaidStatusAfterPaymentChange).
    /// </summary>
    public Payment EditPayment(PaymentId paymentId, Money amountPaid, PaymentMethod method, string notes, string? referenceNumber, DateOnly paymentDate)
    {
        EnsureNotWrittenOff();
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new DomainException("Payment not found on this loan.");

        TotalPaid = TotalPaid.Subtract(payment.AmountPaid).Add(amountPaid);
        payment.Edit(amountPaid, method, notes, referenceNumber, paymentDate);
        Balance = TotalAmountDue.Subtract(TotalPaid);
        RefreshPaidStatusAfterPaymentChange();

        return payment;
    }

    /// <summary>Removes a payment and rolls its AmountPaid back out of TotalPaid/Balance.</summary>
    public void DeletePayment(PaymentId paymentId)
    {
        EnsureNotWrittenOff();
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new DomainException("Payment not found on this loan.");

        _payments.Remove(payment);
        TotalPaid = TotalPaid.Subtract(payment.AmountPaid);
        Balance = TotalAmountDue.Subtract(TotalPaid);
        RefreshPaidStatusAfterPaymentChange();
    }

    /// <summary>
    /// A payment edit/delete can move Balance away from zero (correcting an
    /// over-recorded payment) as easily as RecordPayment moves it to zero —
    /// this is the un-pay side of that same rule. Overdue/Extended/Active
    /// beyond this are re-derived by RefreshOverdueStatus on the next read.
    /// </summary>
    private void RefreshPaidStatusAfterPaymentChange()
    {
        if (Balance.Amount == 0) { Status = LoanStatus.Paid; return; }
        if (Status == LoanStatus.Paid) Status = _extensions.Count > 0 ? LoanStatus.Extended : LoanStatus.Active;
    }

    /// <summary>
    /// Edits an existing extension, rolling its old ExtensionDays/
    /// AdditionalInterestAmount/AdditionalChargesAmount contribution out of
    /// DueDate/TotalInterest/TotalExtensionCharges before applying the new
    /// values — otherwise the old and new contributions would both remain
    /// in effect at once.
    /// </summary>
    public LoanExtension EditExtension(LoanExtensionId extensionId, int extensionDays, Money additionalInterestAmount, Money additionalChargesAmount, string remarks, DateOnly extensionDate)
    {
        EnsureNotWrittenOff();
        var extension = _extensions.FirstOrDefault(e => e.Id == extensionId)
            ?? throw new DomainException("Extension not found on this loan.");

        DueDate = DueDate.AddDays(-extension.ExtensionDays).AddDays(extensionDays);
        TotalInterest = TotalInterest.Subtract(extension.AdditionalInterestAmount).Add(additionalInterestAmount);
        TotalExtensionCharges = TotalExtensionCharges.Subtract(extension.AdditionalChargesAmount).Add(additionalChargesAmount);

        extension.Edit(extensionDays, additionalInterestAmount, additionalChargesAmount, remarks, extensionDate);

        TotalAmountDue = PrincipalAmount.Add(TotalInterest).Add(TotalExtensionCharges);
        Balance = TotalAmountDue.Subtract(TotalPaid);

        return extension;
    }

    /// <summary>Removes an extension and rolls its ExtensionDays/AdditionalInterestAmount/AdditionalChargesAmount contribution back out.</summary>
    public void DeleteExtension(LoanExtensionId extensionId)
    {
        EnsureNotWrittenOff();
        var extension = _extensions.FirstOrDefault(e => e.Id == extensionId)
            ?? throw new DomainException("Extension not found on this loan.");

        _extensions.Remove(extension);
        DueDate = DueDate.AddDays(-extension.ExtensionDays);
        TotalInterest = TotalInterest.Subtract(extension.AdditionalInterestAmount);
        TotalExtensionCharges = TotalExtensionCharges.Subtract(extension.AdditionalChargesAmount);

        TotalAmountDue = PrincipalAmount.Add(TotalInterest).Add(TotalExtensionCharges);
        Balance = TotalAmountDue.Subtract(TotalPaid);
    }

    /// <summary>
    /// Records a payment and recalculates balance/status (SRS 3.4). Fully
    /// paying off the balance moves the loan to Paid regardless of what
    /// status it was in before (Active, Extended, or Overdue).
    /// </summary>
    public Payment RecordPayment(Money amountPaid, PaymentMethod method, string notes, DateOnly paymentDate, string? referenceNumber = null)
    {
        EnsureNotPaid();

        var payment = new Payment(Id, paymentDate, amountPaid, method, notes, referenceNumber);
        _payments.Add(payment);

        TotalPaid = TotalPaid.Add(amountPaid);
        Balance = TotalAmountDue.Subtract(TotalPaid);

        if (Balance.Amount == 0)
            Status = LoanStatus.Paid;

        RaiseDomainEvent(new PaymentRecordedDomainEvent(Id, payment.Id, amountPaid, paymentDate, Balance));
        return payment;
    }

    /// <summary>Loan Details "Documents" tab — allowed regardless of Status; a written-off or paid loan's records still need to be kept.</summary>
    public LoanDocument UploadDocument(string originalFileName, string contentType, byte[] content, string uploadedBy)
    {
        var document = new LoanDocument(Id, originalFileName, contentType, content, uploadedBy);
        _documents.Add(document);
        return document;
    }

    public void DeleteDocument(LoanDocumentId documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new DomainException("Document not found on this loan.");
        _documents.Remove(document);
    }

    /// <summary>
    /// Recomputes whether this loan should be flagged Overdue against a
    /// given "as of" date. Called by the application layer's read path
    /// rather than a background job, since this system doesn't need
    /// real-time push notifications — the status just needs to be correct
    /// whenever someone looks at it.
    /// </summary>
    public void RefreshOverdueStatus(DateOnly asOfDate)
    {
        if (Status is LoanStatus.Paid or LoanStatus.WrittenOff) return;

        Status = asOfDate > DueDate ? LoanStatus.Overdue : (_extensions.Count > 0 ? LoanStatus.Extended : LoanStatus.Active);
    }

    private void EnsureNotPaid()
    {
        if (Status == LoanStatus.Paid)
            throw new DomainException("This loan is already fully paid and cannot be modified further.");
        if (Status == LoanStatus.WrittenOff)
            throw new DomainException("This loan has been written off and cannot be modified further.");
    }

    private void EnsureNotWrittenOff()
    {
        if (Status == LoanStatus.WrittenOff)
            throw new DomainException("This loan has been written off and cannot be modified further.");
    }
}
