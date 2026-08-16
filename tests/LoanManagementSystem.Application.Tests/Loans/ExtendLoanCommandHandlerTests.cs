using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.ExtendLoan;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class ExtendLoanCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ExtendLoanCommandHandler _handler;

    public ExtendLoanCommandHandlerTests()
    {
        _handler = new ExtendLoanCommandHandler(_loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingLoan_ExtendsAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(3500), InterestRate.Default, new DateOnly(2026, 1, 1), 30);
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new ExtendLoanCommand(loan.Id.ToString(), 30, "business slow", 105);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(30, result.ExtensionDays);
        Assert.Equal(105m, result.AdditionalChargesAmount);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new ExtendLoanCommand(LoanId.New().ToString(), 30, "x", 100);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
