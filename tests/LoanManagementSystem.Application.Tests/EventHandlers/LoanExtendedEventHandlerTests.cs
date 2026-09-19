using LoanManagementSystem.Application.EventHandlers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.EventHandlers;

/// <summary>
/// Regression coverage for capturing an extension's own Remarks into its
/// loan_ledger row — before this fix, that row's Remarks was always the
/// generic literal "Extension +N days", so a lender's borrower-facing note
/// like "Additional interest Aug 2-Sep 2" never reached Statement of
/// Account V2's Account Activity table even though it was entered on the
/// extension.
/// </summary>
public class LoanExtendedEventHandlerTests
{
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LoanExtendedEventHandler _handler;

    public LoanExtendedEventHandlerTests()
    {
        _handler = new LoanExtendedEventHandler(_loanLedgerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExtensionWithRemarks_WritesRemarksAsLedgerRemarks()
    {
        LoanLedgerEntry? added = null;
        _loanLedgerRepository.Setup(r => r.Add(It.IsAny<LoanLedgerEntry>())).Callback<LoanLedgerEntry>(e => added = e);

        var notification = new LoanExtendedDomainEvent(
            LoanId.New(), LoanExtensionId.New(), new DateOnly(2026, 9, 4), 30, Money.Of(7000), Money.Of(100_000),
            "Additional interest Aug 2-Sep 2");

        await _handler.Handle(notification, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("Additional interest Aug 2-Sep 2", added!.Remarks);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExtensionWithBlankRemarks_FallsBackToDaysBasedLabel()
    {
        LoanLedgerEntry? added = null;
        _loanLedgerRepository.Setup(r => r.Add(It.IsAny<LoanLedgerEntry>())).Callback<LoanLedgerEntry>(e => added = e);

        var notification = new LoanExtendedDomainEvent(
            LoanId.New(), LoanExtensionId.New(), new DateOnly(2026, 9, 4), 30, Money.Of(7000), Money.Of(100_000), "");

        await _handler.Handle(notification, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("Extension +30 days", added!.Remarks);
    }
}
