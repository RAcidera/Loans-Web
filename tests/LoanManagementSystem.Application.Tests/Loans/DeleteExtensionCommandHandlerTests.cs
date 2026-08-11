using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.DeleteExtension;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class DeleteExtensionCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteExtensionCommandHandler _handler;

    public DeleteExtensionCommandHandlerTests()
    {
        _handler = new DeleteExtensionCommandHandler(_loanRepository.Object, _customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingExtension_DeletesAndReturnsUpdatedLoan()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 30);
        var dueDateBeforeExtension = loan.DueDate;
        var extension = loan.Extend(15, Money.Of(50), Money.Of(10), "temporary", new DateOnly(2026, 1, 20));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _handler.Handle(new DeleteExtensionCommand(loan.Id.ToString(), extension.Id.ToString()), CancellationToken.None);

        Assert.Equal(dueDateBeforeExtension.ToString("yyyy-MM-dd"), result.DueDate);
        Assert.Equal(0m, result.TotalInterest);
        Assert.Equal(0m, result.TotalExtensionCharges);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteExtensionCommand(LoanId.New().ToString(), LoanExtensionId.New().ToString()), CancellationToken.None));
    }
}
