using LoanManagementSystem.Application.EventHandlers;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.EventHandlers;

/// <summary>
/// Regression coverage for the SOA running-balance bug caused by editing a
/// loan's disbursement date: before this fix, correcting a loan's StartDate
/// left the loan_ledger's LoanReleased/InterestAdded rows stamped with the
/// OLD date, so GenerateLoanSoaQuery's chronological recompute could sort a
/// payment "before" the loan it belongs to, producing a nonsensical negative
/// running balance for that row. See LoanOriginationEditedDomainEvent's doc
/// comment for why revising those rows in place is now safe.
/// </summary>
public class LoanOriginationEditedEventHandlerTests
{
    private readonly Mock<ICashLedgerRepository> _cashLedgerRepository = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly LoanOriginationEditedEventHandler _handler;

    public LoanOriginationEditedEventHandlerTests()
    {
        _handler = new LoanOriginationEditedEventHandler(_cashLedgerRepository.Object, _loanLedgerRepository.Object, _loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_RevisesLoanReleasedAndInterestAddedRows_ToNewDateAndCurrentAmounts()
    {
        var customer = Customer.Create("Dana Reyes", "9 Luna St.", "+63 917 555 1122", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1200), InterestRate.Of(0.03m), new DateOnly(2026, 3, 1), 60);
        loan.EditLoan(new DateOnly(2026, 1, 1), null, null, null, null, "admin", principal: Money.Of(1200));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var releaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1200), Money.Zero, Money.Of(1200),
            "Loan released", new DateOnly(2026, 3, 1));
        var interestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(36), Money.Zero, Money.Of(1236),
            "Interest added", new DateOnly(2026, 3, 1));
        var cashEntry = CashLedgerEntry.Record(CashTransactionType.LoanRelease, Money.Of(1200), "Loan release", new DateOnly(2026, 3, 1), referenceId: "LM-000001");

        _loanLedgerRepository.Setup(r => r.GetByLoanIdAndTypeAsync(loan.Id, LoanLedgerTransactionType.LoanReleased, It.IsAny<CancellationToken>()))
            .ReturnsAsync(releaseEntry);
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAndTypeAsync(loan.Id, LoanLedgerTransactionType.InterestAdded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(interestEntry);
        _cashLedgerRepository.Setup(r => r.GetLoanReleaseEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashEntry);

        var notification = new LoanOriginationEditedDomainEvent(loan.Id, loan.PrincipalAmount, loan.StartDate);
        await _handler.Handle(notification, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 1, 1), releaseEntry.TransactionDate);
        Assert.Equal(1200m, releaseEntry.Debit.Amount);
        Assert.Equal(new DateOnly(2026, 1, 1), interestEntry.TransactionDate);
        Assert.Equal(loan.TotalInterest.Amount, interestEntry.Debit.Amount);
        Assert.Equal(new DateOnly(2026, 1, 1), cashEntry.TransactionDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Regression test: an interest-only correction (Principal/StartDate
    /// unchanged) still reaches this handler now — see
    /// Loan.EditLoan's broadened raise condition — and must revise the
    /// InterestAdded row's Debit to the new amount even though the
    /// LoanReleased row's own Debit/TransactionDate don't actually change.
    /// </summary>
    [Fact]
    public async Task Handle_InterestOnlyEdit_RevisesInterestAddedRowsDebit()
    {
        var customer = Customer.Create("Elena Cruz", "5 Bonifacio St.", "+63 917 444 7788", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1), 60);
        loan.EditLoan(startDate: null, dueDate: null, interestRate: null, interestAmount: Money.Of(80), remarks: null, editedBy: "admin");

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var releaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 1, 1));
        var interestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030),
            "Interest added", new DateOnly(2026, 1, 1));

        _loanLedgerRepository.Setup(r => r.GetByLoanIdAndTypeAsync(loan.Id, LoanLedgerTransactionType.LoanReleased, It.IsAny<CancellationToken>()))
            .ReturnsAsync(releaseEntry);
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAndTypeAsync(loan.Id, LoanLedgerTransactionType.InterestAdded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(interestEntry);
        _cashLedgerRepository.Setup(r => r.GetLoanReleaseEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashLedgerEntry?)null);

        var notification = new LoanOriginationEditedDomainEvent(loan.Id, loan.PrincipalAmount, loan.StartDate);
        await _handler.Handle(notification, CancellationToken.None);

        Assert.Equal(80m, interestEntry.Debit.Amount);
        Assert.Equal(1000m, releaseEntry.Debit.Amount); // unchanged, principal wasn't edited
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoMatchingRows_DoesNotSaveChanges()
    {
        var loanId = LoanId.New();
        _loanRepository.Setup(r => r.GetByIdAsync(loanId, It.IsAny<CancellationToken>())).ReturnsAsync((Loan?)null);
        _cashLedgerRepository.Setup(r => r.GetLoanReleaseEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CashLedgerEntry?)null);
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAndTypeAsync(loanId, LoanLedgerTransactionType.LoanReleased, It.IsAny<CancellationToken>())).ReturnsAsync((LoanLedgerEntry?)null);

        var notification = new LoanOriginationEditedDomainEvent(loanId, Money.Of(1000), new DateOnly(2026, 1, 1));
        await _handler.Handle(notification, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
