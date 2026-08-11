using LoanManagementSystem.Application.Loans.Queries.GetDashboardReceivables;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GetDashboardReceivablesQueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly GetDashboardReceivablesQueryHandler _handler;

    public GetDashboardReceivablesQueryHandlerTests()
    {
        _handler = new GetDashboardReceivablesQueryHandler(_loanRepository.Object);
    }

    [Fact]
    public async Task Handle_ComputesGrossCollectibleAndBadLoanReceivables()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Active, normal classification: 1000 @ 3% => balance 1030.
        var active = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, today, 60);

        // Overdue, classified Bad Loan: 2000 @ 3% => balance 2060.
        var badLoan = Loan.Originate(CustomerId.New(), Money.Of(2000), InterestRate.Default, today.AddDays(-90), 30);
        badLoan.RefreshOverdueStatus(today);
        badLoan.ChangeClassification(LoanClassification.BadLoan, "admin");

        // Written off: 500 @ 3% => balance 515, must be excluded from Gross Receivables.
        var writtenOff = Loan.Originate(CustomerId.New(), Money.Of(500), InterestRate.Default, today.AddDays(-200), 30);
        writtenOff.WriteOff("admin");

        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { active, badLoan, writtenOff });

        var result = await _handler.Handle(new GetDashboardReceivablesQuery(), CancellationToken.None);

        Assert.Equal(1030m + 2060m, result.GrossReceivables);
        Assert.Equal(2060m, result.BadLoanReceivables);
        Assert.Equal(1030m, result.CollectibleReceivables);
        Assert.Equal(1, result.ActiveLoansCount);
        Assert.Equal(1, result.OverdueLoansCount);
        Assert.Equal(1, result.WrittenOffLoansCount);
    }

    [Fact]
    public async Task Handle_CountsLoansDueWithinTheNextSevenDays()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dueSoon = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, today.AddDays(-58), 60); // due in 2 days
        var dueLater = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, today, 60); // due in 60 days

        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { dueSoon, dueLater });

        var result = await _handler.Handle(new GetDashboardReceivablesQuery(), CancellationToken.None);

        Assert.Equal(1, result.LoansDueThisWeekCount);
    }

    [Fact]
    public async Task Handle_NoLoans_ReturnsAllZeros()
    {
        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Loan>());

        var result = await _handler.Handle(new GetDashboardReceivablesQuery(), CancellationToken.None);

        Assert.Equal(0m, result.GrossReceivables);
        Assert.Equal(0m, result.CollectibleReceivables);
        Assert.Equal(0m, result.BadLoanReceivables);
        Assert.Equal(0, result.LoansDueThisWeekCount);
    }
}
