using LoanManagementSystem.Application.Loans.Queries.GetCustomerReceivables;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GetCustomerReceivablesQueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly GetCustomerReceivablesQueryHandler _handler;

    public GetCustomerReceivablesQueryHandlerTests()
    {
        _handler = new GetCustomerReceivablesQueryHandler(_loanRepository.Object);
    }

    [Fact]
    public async Task Handle_ComputesReceivables_ScopedToGivenCustomerOnly()
    {
        var customerId = CustomerId.New();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // This customer: one active (1030) and one bad-loan-classified (2060).
        var active = Loan.Originate(customerId, Money.Of(1000), InterestRate.Default, today, 60);
        var badLoan = Loan.Originate(customerId, Money.Of(2000), InterestRate.Default, today.AddDays(-90), 30);
        badLoan.RefreshOverdueStatus(today);
        badLoan.ChangeClassification(LoanClassification.BadLoan, "admin");

        // GetByCustomerAsync is scoped by the repository itself — the mock only returns this customer's loans,
        // proving the handler doesn't need its own extra filtering to exclude some other customer's loan.
        _loanRepository.Setup(r => r.GetByCustomerAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { active, badLoan });

        var result = await _handler.Handle(new GetCustomerReceivablesQuery(customerId.ToString()), CancellationToken.None);

        Assert.Equal(1030m + 2060m, result.GrossReceivables);
        Assert.Equal(2060m, result.BadLoanReceivables);
        Assert.Equal(1030m, result.CollectibleReceivables);
        Assert.Equal(1, result.ActiveLoansCount);
        Assert.Equal(1, result.OverdueLoansCount);
    }

    [Fact]
    public async Task Handle_CustomerWithNoLoans_ReturnsAllZeros()
    {
        _loanRepository.Setup(r => r.GetByCustomerAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan>());

        var result = await _handler.Handle(new GetCustomerReceivablesQuery(CustomerId.New().ToString()), CancellationToken.None);

        Assert.Equal(0m, result.GrossReceivables);
        Assert.Equal(0m, result.CollectibleReceivables);
        Assert.Equal(0m, result.BadLoanReceivables);
        Assert.Equal(0, result.ActiveLoansCount);
    }
}
