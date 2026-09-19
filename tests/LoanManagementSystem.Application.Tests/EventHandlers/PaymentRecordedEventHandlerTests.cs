using LoanManagementSystem.Application.EventHandlers;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.EventHandlers;

/// <summary>
/// Regression coverage for capturing a payment's own Notes into its
/// mirroring loan_ledger row's Remarks — before this fix, that row's
/// Remarks was always the generic literal "Payment received", so
/// borrower-facing notes like "Interest only" or "Partial payment" never
/// reached Statement of Account V2's Account Activity table even though
/// they were entered on the payment.
/// </summary>
public class PaymentRecordedEventHandlerTests
{
    private readonly Mock<ICashLedgerRepository> _cashLedgerRepository = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PaymentRecordedEventHandler _handler;

    public PaymentRecordedEventHandlerTests()
    {
        _handler = new PaymentRecordedEventHandler(_cashLedgerRepository.Object, _loanLedgerRepository.Object, _loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_PaymentWithNotes_WritesNotesAsLedgerRemarks()
    {
        LoanLedgerEntry? added = null;
        _loanLedgerRepository.Setup(r => r.Add(It.IsAny<LoanLedgerEntry>())).Callback<LoanLedgerEntry>(e => added = e);

        var notification = new PaymentRecordedDomainEvent(
            LoanId.New(), PaymentId.New(), Money.Of(7000), new DateOnly(2026, 8, 4), Money.Of(100_000), "Interest only");

        await _handler.Handle(notification, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("Interest only", added!.Remarks);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentWithBlankNotes_FallsBackToGenericLabel()
    {
        LoanLedgerEntry? added = null;
        _loanLedgerRepository.Setup(r => r.Add(It.IsAny<LoanLedgerEntry>())).Callback<LoanLedgerEntry>(e => added = e);

        var notification = new PaymentRecordedDomainEvent(
            LoanId.New(), PaymentId.New(), Money.Of(1000), new DateOnly(2026, 8, 4), Money.Of(99_000), "");

        await _handler.Handle(notification, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("Payment received", added!.Remarks);
    }
}
