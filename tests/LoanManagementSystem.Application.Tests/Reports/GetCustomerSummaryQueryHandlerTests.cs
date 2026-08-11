using LoanManagementSystem.Application.Reports.Queries.GetCustomerSummary;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Reports;

public class GetCustomerSummaryQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly GetCustomerSummaryQueryHandler _handler;

    public GetCustomerSummaryQueryHandlerTests()
    {
        _handler = new GetCustomerSummaryQueryHandler(_customerRepository.Object, _loanRepository.Object);
    }

    [Fact]
    public async Task Handle_SumsBorrowedAndPaidPerCustomer()
    {
        var maria = Customer.Create("Maria Santos", "", "", "Fish vendor");
        var jun = Customer.Create("Jun Dela Cruz", "", "", "Vegetable vendor");

        var loan1 = Loan.Originate(maria.Id, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan1.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 2, 1));
        var loan2 = Loan.Originate(maria.Id, Money.Of(2000), InterestRate.Of(0), new DateOnly(2026, 3, 1));

        _customerRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { maria, jun });
        _loanRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { loan1, loan2 });

        var result = await _handler.Handle(new GetCustomerSummaryQuery(), CancellationToken.None);

        var mariaSummary = result.Single(r => r.CustomerId == maria.Id.ToString());
        Assert.Equal(3000m, mariaSummary.TotalBorrowed);
        Assert.Equal(400m, mariaSummary.TotalPaid);
        Assert.Equal(2, mariaSummary.LoansCount);

        var junSummary = result.Single(r => r.CustomerId == jun.Id.ToString());
        Assert.Equal(0m, junSummary.TotalBorrowed);
        Assert.Equal(0, junSummary.LoansCount);
    }
}
