using LoanManagementSystem.Application.EventHandlers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.EventHandlers;

/// <summary>
/// Regression coverage for keeping a payment's loan_ledger Remarks in sync
/// when the payment is edited — an edit that changes the notes text (e.g.
/// correcting "Interst only" to "Interest only") must reach the ledger row
/// too, since Statement of Account V2 reads Remarks from the ledger.
/// </summary>
public class PaymentEditedEventHandlerTests
{
    private readonly Mock<ICashLedgerRepository> _cashLedgerRepository = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PaymentEditedEventHandler _handler;

    public PaymentEditedEventHandlerTests()
    {
        _handler = new PaymentEditedEventHandler(_cashLedgerRepository.Object, _loanLedgerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_MatchingLedgerRow_RevisesRemarksToNewNotes()
    {
        var loanId = LoanId.New();
        var paymentId = PaymentId.New();
        var entry = LoanLedgerEntry.Record(
            loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(500), Money.Of(9500),
            "Payment received", new DateOnly(2026, 2, 1), referenceId: paymentId.ToString());

        _loanLedgerRepository.Setup(r => r.GetByPaymentReferenceAsync(loanId, paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var notification = new PaymentEditedDomainEvent(loanId, paymentId, Money.Of(600), new DateOnly(2026, 2, 2), Money.Of(9400), "Corrected: interest only");
        await _handler.Handle(notification, CancellationToken.None);

        Assert.Equal(600m, entry.Credit.Amount);
        Assert.Equal("Corrected: interest only", entry.Remarks);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MatchingLedgerRow_BlankNewNotes_FallsBackToGenericLabel()
    {
        var loanId = LoanId.New();
        var paymentId = PaymentId.New();
        var entry = LoanLedgerEntry.Record(
            loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(500), Money.Of(9500),
            "Interest only", new DateOnly(2026, 2, 1), referenceId: paymentId.ToString());

        _loanLedgerRepository.Setup(r => r.GetByPaymentReferenceAsync(loanId, paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var notification = new PaymentEditedDomainEvent(loanId, paymentId, Money.Of(500), new DateOnly(2026, 2, 1), Money.Of(9500), "");
        await _handler.Handle(notification, CancellationToken.None);

        Assert.Equal("Payment received", entry.Remarks);
    }

    [Fact]
    public async Task Handle_NoMatchingRow_DoesNotSaveChanges()
    {
        _loanLedgerRepository.Setup(r => r.GetByPaymentReferenceAsync(It.IsAny<LoanId>(), It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoanLedgerEntry?)null);

        var notification = new PaymentEditedDomainEvent(LoanId.New(), PaymentId.New(), Money.Of(500), new DateOnly(2026, 2, 1), Money.Of(9500), "notes");
        await _handler.Handle(notification, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
