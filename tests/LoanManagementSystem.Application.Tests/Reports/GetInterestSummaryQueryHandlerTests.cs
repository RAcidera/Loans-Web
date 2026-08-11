using LoanManagementSystem.Application.Reports.Queries.GetInterestSummary;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Reports;

public class GetInterestSummaryQueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly GetInterestSummaryQueryHandler _handler;

    public GetInterestSummaryQueryHandlerTests()
    {
        _handler = new GetInterestSummaryQueryHandler(_loanRepository.Object);
    }

    [Fact]
    public async Task Handle_SumsInterestAcrossActiveExtendedAndPaidLoans()
    {
        // 1000 @ 3% => 30 interest, stays Active.
        var active = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));

        // 2000 @ 3% => 60 interest, +20 extension fee => 80, becomes Extended.
        var extended = Loan.Originate(CustomerId.New(), Money.Of(2000), InterestRate.Default, new DateOnly(2026, 1, 1));
        extended.Extend(15, Money.Of(20), Money.Zero, "late", new DateOnly(2026, 3, 1));

        // 500 @ 3% => 15 interest, fully paid off => Paid.
        var paid = Loan.Originate(CustomerId.New(), Money.Of(500), InterestRate.Default, new DateOnly(2026, 1, 1));
        paid.RecordPayment(Money.Of(515), PaymentMethod.Cash, "", new DateOnly(2026, 2, 1));

        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { active, extended, paid });

        var result = await _handler.Handle(new GetInterestSummaryQuery(), CancellationToken.None);

        Assert.Equal(30m + 80m + 15m, result.TotalInterestEarned);
    }

    [Fact]
    public async Task Handle_NoLoans_ReturnsZero()
    {
        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Loan>());

        var result = await _handler.Handle(new GetInterestSummaryQuery(), CancellationToken.None);

        Assert.Equal(0m, result.TotalInterestEarned);
    }
}
