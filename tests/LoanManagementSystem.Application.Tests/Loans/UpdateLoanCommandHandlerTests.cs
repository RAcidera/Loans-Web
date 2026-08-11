using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.UpdateLoan;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class UpdateLoanCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateLoanCommandHandler _handler;

    public UpdateLoanCommandHandlerTests()
    {
        _handler = new UpdateLoanCommandHandler(_loanRepository.Object, _customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OverridesInterestAmountAndRemarks_Saves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(5000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UpdateLoanCommand(loan.Id.ToString(), "admin", InterestAmount: 50m, Remarks: "goodwill discount");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(50m, result.TotalInterest);
        Assert.Equal("goodwill discount", result.Remarks);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new UpdateLoanCommand(LoanId.New().ToString(), "admin", Remarks: "x");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
