using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.Loans;

public class LoanTests
{
    private static readonly CustomerId SomeCustomer = CustomerId.New();

    // --- Origination (SRS 3.2) ---

    [Fact]
    public void Originate_DefaultTerm_Is60Days()
    {
        var start = new DateOnly(2026, 1, 1);
        var loan = Loan.Originate(SomeCustomer, Money.Of(5000), InterestRate.Default, start);

        Assert.Equal(start.AddDays(60), loan.DueDate);
    }

    [Fact]
    public void Originate_CalculatesInterestAndTotalDue()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(5000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1));

        Assert.Equal(150m, loan.TotalInterest.Amount);   // 5000 * 3%
        Assert.Equal(5150m, loan.TotalAmountDue.Amount); // principal + interest
        Assert.Equal(5150m, loan.Balance.Amount);        // nothing paid yet
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void Originate_ZeroOrNegativePrincipal_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Loan.Originate(SomeCustomer, Money.Of(0), InterestRate.Default, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Originate_RaisesLoanCreatedDomainEvent_WithStartDate_NotWallClockTime()
    {
        // Regression test for a real bug caught during review: the event
        // must carry the loan's own StartDate (for correct ledger dating,
        // including backdated/seeded loans), not just "when this ran".
        var start = new DateOnly(2020, 5, 1); // deliberately not "today"
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, start);

        var raised = Assert.Single(loan.DomainEvents);
        var createdEvent = Assert.IsType<LoanCreatedDomainEvent>(raised);
        Assert.Equal(start, createdEvent.StartDate);
    }

    // --- Payments (SRS 3.4) ---

    [Fact]
    public void RecordPayment_PartialPayment_ReducesBalance_KeepsActive()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(5000), InterestRate.Default, new DateOnly(2026, 1, 1));
        // Balance is 5150 after origination.

        loan.RecordPayment(Money.Of(2000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 15));

        Assert.Equal(2000m, loan.TotalPaid.Amount);
        Assert.Equal(3150m, loan.Balance.Amount);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void RecordPayment_FullPayment_MovesStatusToPaid()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        // 0% interest => balance is exactly 1000.

        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "Full settlement", new DateOnly(2026, 1, 10));

        Assert.Equal(0m, loan.Balance.Amount);
        Assert.Equal(LoanStatus.Paid, loan.Status);
    }

    [Fact]
    public void RecordPayment_MultiplePartials_SumCorrectly()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(3000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1));
        // Balance 3090.

        loan.RecordPayment(Money.Of(1545), PaymentMethod.Cash, "", new DateOnly(2026, 2, 1));
        loan.RecordPayment(Money.Of(1545), PaymentMethod.Cash, "", new DateOnly(2026, 2, 14));

        Assert.Equal(0m, loan.Balance.Amount);
        Assert.Equal(LoanStatus.Paid, loan.Status);
        Assert.Equal(2, loan.Payments.Count);
    }

    [Fact]
    public void RecordPayment_OnAlreadyPaidLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        Assert.Throws<DomainException>(() =>
            loan.RecordPayment(Money.Of(100), PaymentMethod.Cash, "", new DateOnly(2026, 1, 20)));
    }

    [Fact]
    public void RecordPayment_ZeroOrNegativeAmount_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() =>
            loan.RecordPayment(Money.Of(0), PaymentMethod.Cash, "", new DateOnly(2026, 1, 5)));
    }

    [Fact]
    public void RecordPayment_RaisesPaymentRecordedDomainEvent_WithGivenPaymentDate()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        var paymentDate = new DateOnly(2026, 3, 15);

        var payment = loan.RecordPayment(Money.Of(500), PaymentMethod.GCash, "", paymentDate);

        var raised = loan.DomainEvents.OfType<PaymentRecordedDomainEvent>().Single();
        Assert.Equal(payment.Id, raised.PaymentId);
        Assert.Equal(paymentDate, raised.PaymentDate);
        Assert.Equal(500m, raised.AmountPaid.Amount);
    }

    // --- Extensions (SRS 3.3) ---

    [Fact]
    public void Extend_PushesOutDueDate_AddsFee_MarksExtended()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(3500), InterestRate.Default, new DateOnly(2026, 4, 15), 75);
        var dueBeforeExtension = loan.DueDate;
        var balanceBeforeExtension = loan.Balance.Amount;

        loan.Extend(30, Money.Of(105), "Business slow this week", new DateOnly(2026, 6, 29));

        Assert.Equal(dueBeforeExtension.AddDays(30), loan.DueDate);
        Assert.Equal(105m, loan.TotalExtensionCharges.Amount);
        Assert.Equal(balanceBeforeExtension + 105m, loan.Balance.Amount);
        Assert.Equal(LoanStatus.Extended, loan.Status);
        Assert.Single(loan.Extensions);
    }

    [Fact]
    public void Extend_OnAlreadyPaidLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        Assert.Throws<DomainException>(() =>
            loan.Extend(30, Money.Of(50), "too late", new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public void Extend_ZeroOrNegativeDays_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() =>
            loan.Extend(0, Money.Of(50), "invalid", new DateOnly(2026, 2, 1)));
    }

    // --- Overdue status (used by GetLoansQuery on every read) ---

    [Fact]
    public void RefreshOverdueStatus_PastDueDate_UnpaidLoan_BecomesOverdue()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        // Due date is 2026-01-31.

        loan.RefreshOverdueStatus(new DateOnly(2026, 2, 15));

        Assert.Equal(LoanStatus.Overdue, loan.Status);
    }

    [Fact]
    public void RefreshOverdueStatus_BeforeDueDate_StaysActive()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 60);

        loan.RefreshOverdueStatus(new DateOnly(2026, 1, 15));

        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void RefreshOverdueStatus_PastDueDate_ButFullyPaid_StaysPaid()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 30);
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 20));

        loan.RefreshOverdueStatus(new DateOnly(2026, 3, 1)); // long past due date

        Assert.Equal(LoanStatus.Paid, loan.Status);
    }

    [Fact]
    public void RefreshOverdueStatus_PastDueDate_WithPriorExtension_BecomesOverdue_NotExtended()
    {
        // An extension pushes the due date out once; if the *new* due date
        // also passes unpaid, the loan should read as Overdue, not sit at
        // Extended forever.
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        loan.Extend(30, Money.Of(30), "grace period", new DateOnly(2026, 1, 31));
        // New due date: 2026-03-02

        loan.RefreshOverdueStatus(new DateOnly(2026, 4, 1));

        Assert.Equal(LoanStatus.Overdue, loan.Status);
    }

    // --- Loan Classification (user-managed, separate from Status) ---

    [Fact]
    public void Originate_DefaultsClassificationToNormal()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Equal(LoanClassification.Normal, loan.Classification);
    }

    [Fact]
    public void ChangeClassification_ToBadLoan_DoesNotAffectStatus()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        loan.RefreshOverdueStatus(new DateOnly(2026, 2, 15)); // becomes Overdue

        loan.ChangeClassification(LoanClassification.BadLoan, "admin");

        Assert.Equal(LoanClassification.BadLoan, loan.Classification);
        Assert.Equal(LoanStatus.Overdue, loan.Status); // Status and Classification move independently
    }

    [Fact]
    public void ChangeClassification_RaisesLoanClassificationChangedDomainEvent()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        loan.ChangeClassification(LoanClassification.WatchList, "admin");

        var raised = loan.DomainEvents.OfType<LoanClassificationChangedDomainEvent>().Single();
        Assert.Equal(LoanClassification.Normal, raised.OldClassification);
        Assert.Equal(LoanClassification.WatchList, raised.NewClassification);
        Assert.Equal("admin", raised.ChangedBy);
    }

    // --- Write Off ---

    [Fact]
    public void WriteOff_SetsStatusToWrittenOff_RaisesDomainEvent()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        loan.WriteOff("admin");

        Assert.Equal(LoanStatus.WrittenOff, loan.Status);
        Assert.Contains(loan.DomainEvents, e => e is LoanWrittenOffDomainEvent);
    }

    [Fact]
    public void WriteOff_OnFullyPaidLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        Assert.Throws<DomainException>(() => loan.WriteOff("admin"));
    }

    [Fact]
    public void RefreshOverdueStatus_OnWrittenOffLoan_StaysWrittenOff()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        loan.WriteOff("admin");

        loan.RefreshOverdueStatus(new DateOnly(2026, 3, 1)); // long past due date

        Assert.Equal(LoanStatus.WrittenOff, loan.Status);
    }

    // --- EditLoan (goodwill discount / post-creation overrides) ---

    [Fact]
    public void EditLoan_OverridesInterestAmountDirectly_RecalculatesBalance()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(5000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1));
        // Balance is 5150 (5000 + 150 interest).

        loan.EditLoan(startDate: null, dueDate: null, interestRate: null, interestAmount: Money.Of(50), remarks: "Goodwill discount for early payoff", editedBy: "admin");

        Assert.Equal(50m, loan.TotalInterest.Amount);
        Assert.Equal(5050m, loan.TotalAmountDue.Amount);
        Assert.Equal(5050m, loan.Balance.Amount);
        Assert.Equal("Goodwill discount for early payoff", loan.Remarks);
    }

    [Fact]
    public void EditLoan_OverridesInterestRateOnly_RecomputesInterestFromNewRate()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(5000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1));
        loan.Extend(30, Money.Of(40), "grace", new DateOnly(2026, 2, 1));
        // Extensions only add Additional Charges now, never interest — TotalInterest stays at 150 (origination only).

        loan.EditLoan(startDate: null, dueDate: null, interestRate: InterestRate.Of(0.01m), interestAmount: null, remarks: null, editedBy: "admin");

        // New origination interest: 5000 * 1% = 50.
        Assert.Equal(50m, loan.TotalInterest.Amount);
        Assert.Equal(0.01m, loan.InterestRate.Value);
    }

    [Fact]
    public void EditLoan_OverridesStartAndDueDate()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 60);

        loan.EditLoan(startDate: new DateOnly(2026, 1, 5), dueDate: new DateOnly(2026, 2, 5), interestRate: null, interestAmount: null, remarks: null, editedBy: "admin");

        Assert.Equal(new DateOnly(2026, 1, 5), loan.StartDate);
        Assert.Equal(new DateOnly(2026, 2, 5), loan.DueDate);
    }

    [Fact]
    public void EditLoan_DueDateBeforeStartDate_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 60);

        Assert.Throws<DomainException>(() =>
            loan.EditLoan(startDate: null, dueDate: new DateOnly(2025, 12, 1), interestRate: null, interestAmount: null, remarks: null, editedBy: "admin"));
    }

    [Fact]
    public void EditLoan_OnFullyPaidLoan_IsAllowed()
    {
        // Corrections after the fact should still be possible — only WrittenOff blocks EditLoan.
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        loan.EditLoan(startDate: null, dueDate: null, interestRate: null, interestAmount: null, remarks: "corrected data entry", editedBy: "admin");

        Assert.Equal("corrected data entry", loan.Remarks);
    }

    [Fact]
    public void EditLoan_OnWrittenOffLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        loan.WriteOff("admin");

        Assert.Throws<DomainException>(() =>
            loan.EditLoan(startDate: null, dueDate: null, interestRate: null, interestAmount: null, remarks: "too late", editedBy: "admin"));
    }

    // --- EditPayment / DeletePayment ---

    [Fact]
    public void EditPayment_ChangesAmount_RecalculatesTotalPaidAndBalance()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        loan.EditPayment(payment.Id, Money.Of(600), PaymentMethod.GCash, "corrected", "REF-1", new DateOnly(2026, 1, 11));

        Assert.Equal(600m, loan.TotalPaid.Amount);
        Assert.Equal(400m, loan.Balance.Amount);
        Assert.Equal(PaymentMethod.GCash, payment.PaymentMethod);
        Assert.Equal("REF-1", payment.ReferenceNumber);
    }

    [Fact]
    public void EditPayment_ReducingAmountBelowBalance_UnpaysAFullyPaidLoan()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));
        Assert.Equal(LoanStatus.Paid, loan.Status);

        loan.EditPayment(payment.Id, Money.Of(700), PaymentMethod.Cash, "correction: overstated", null, new DateOnly(2026, 1, 10));

        Assert.Equal(300m, loan.Balance.Amount);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void DeletePayment_RemovesItAndRollsBackTotalPaid()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));

        loan.DeletePayment(payment.Id);

        Assert.Empty(loan.Payments);
        Assert.Equal(0m, loan.TotalPaid.Amount);
        Assert.Equal(1000m, loan.Balance.Amount);
    }

    [Fact]
    public void DeletePayment_UnknownPaymentId_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() => loan.DeletePayment(PaymentId.New()));
    }

    [Fact]
    public void EditPayment_OnWrittenOffLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(500), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));
        loan.WriteOff("admin");

        Assert.Throws<DomainException>(() =>
            loan.EditPayment(payment.Id, Money.Of(600), PaymentMethod.Cash, "", null, new DateOnly(2026, 1, 10)));
    }

    // --- EditExtension / DeleteExtension ---

    [Fact]
    public void EditExtension_ChangesDaysAndAmounts_RollsBackOldContributionFirst()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 30);
        // Due date 2026-01-31, balance 1000.
        var extension = loan.Extend(10, Money.Of(20), "initial", new DateOnly(2026, 1, 20));
        // Due date now 2026-02-10, TotalExtensionCharges 20, balance 1020.

        loan.EditExtension(extension.Id, 20, Money.Of(30), "revised", new DateOnly(2026, 1, 21));

        Assert.Equal(new DateOnly(2026, 2, 20), loan.DueDate); // 2026-01-31 rolled back to, then +20
        Assert.Equal(30m, loan.TotalExtensionCharges.Amount);
        Assert.Equal(1030m, loan.Balance.Amount); // 1000 + 30
        Assert.Equal(20, extension.ExtensionDays);
        Assert.Equal("revised", extension.Remarks);
    }

    [Fact]
    public void DeleteExtension_RevertsDueDateAndCharges()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 30);
        var dueDateBeforeExtension = loan.DueDate;
        var extension = loan.Extend(15, Money.Of(25), "temporary", new DateOnly(2026, 1, 20));

        loan.DeleteExtension(extension.Id);

        Assert.Empty(loan.Extensions);
        Assert.Equal(dueDateBeforeExtension, loan.DueDate);
        Assert.Equal(0m, loan.TotalExtensionCharges.Amount);
        Assert.Equal(1000m, loan.Balance.Amount);
    }

    [Fact]
    public void DeleteExtension_UnknownExtensionId_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() => loan.DeleteExtension(LoanExtensionId.New()));
    }

    [Fact]
    public void EditExtension_OnWrittenOffLoan_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        var extension = loan.Extend(10, Money.Of(10), "x", new DateOnly(2026, 1, 15));
        loan.WriteOff("admin");

        Assert.Throws<DomainException>(() =>
            loan.EditExtension(extension.Id, 20, Money.Of(20), "y", new DateOnly(2026, 1, 16)));
    }

    // --- Documents (Loan Details "Documents" tab) ---

    [Fact]
    public void UploadDocument_AddsToDocuments()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        var document = loan.UploadDocument("agreement.pdf", "application/pdf", new byte[] { 1, 2, 3, 4 }, "admin");

        Assert.Single(loan.Documents);
        Assert.Equal("agreement.pdf", document.OriginalFileName);
        Assert.Equal(4, document.FileSizeBytes);
    }

    [Fact]
    public void UploadDocument_OnWrittenOffLoan_IsStillAllowed()
    {
        // Documents are records to keep, not financial mutations — unlike
        // Extend()/RecordPayment(), a written-off loan's paperwork should
        // still be attachable/removable.
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        loan.WriteOff("admin");

        var document = loan.UploadDocument("agreement.pdf", "application/pdf", new byte[] { 1 }, "admin");

        Assert.Single(loan.Documents);
        Assert.NotNull(document);
    }

    [Fact]
    public void DeleteDocument_RemovesIt()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        var document = loan.UploadDocument("agreement.pdf", "application/pdf", new byte[] { 1 }, "admin");

        loan.DeleteDocument(document.Id);

        Assert.Empty(loan.Documents);
    }

    [Fact]
    public void DeleteDocument_UnknownId_Throws()
    {
        var loan = Loan.Originate(SomeCustomer, Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        Assert.Throws<DomainException>(() => loan.DeleteDocument(LoanDocumentId.New()));
    }
}
