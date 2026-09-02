using LoanManagementSystem.Application.EventHandlers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.EventHandlers;

/// <summary>
/// Regression coverage for editing an extension's fee/date: before this
/// fix, EditExtension raised no domain event at all, so the mirroring
/// loan_ledger Extension row kept its stale Debit/TransactionDate forever,
/// leaving every payment recorded after that extension showing a running
/// balance off by (new charge - old charge) even though the loan's own
/// Balance was correct.
/// </summary>
public class LoanExtensionEditedEventHandlerTests
{
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LoanExtensionEditedEventHandler _handler;

    public LoanExtensionEditedEventHandlerTests()
    {
        _handler = new LoanExtensionEditedEventHandler(_loanLedgerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_RevisesMatchingExtensionRow_ToNewChargeAndDate()
    {
        var loanId = LoanId.New();
        var extensionId = LoanExtensionId.New();
        var entry = LoanLedgerEntry.Record(
            loanId, LoanLedgerTransactionType.Extension, Money.Of(20), Money.Zero, Money.Of(1020),
            "Extension +10 days", new DateOnly(2026, 1, 20), referenceId: extensionId.ToString());

        _loanLedgerRepository.Setup(r => r.GetByReferenceIdAsync(loanId, extensionId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var notification = new LoanExtensionEditedDomainEvent(loanId, extensionId, Money.Of(35), new DateOnly(2026, 1, 18));
        await _handler.Handle(notification, CancellationToken.None);

        Assert.Equal(35m, entry.Debit.Amount);
        Assert.Equal(new DateOnly(2026, 1, 18), entry.TransactionDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoMatchingRow_DoesNotSaveChanges()
    {
        var loanId = LoanId.New();
        var extensionId = LoanExtensionId.New();
        _loanLedgerRepository.Setup(r => r.GetByReferenceIdAsync(loanId, extensionId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoanLedgerEntry?)null);

        var notification = new LoanExtensionEditedDomainEvent(loanId, extensionId, Money.Of(35), new DateOnly(2026, 1, 18));
        await _handler.Handle(notification, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
