using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.ChangeLoanClassification;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class ChangeLoanClassificationCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ChangeLoanClassificationCommandHandler _handler;

    public ChangeLoanClassificationCommandHandlerTests()
    {
        _handler = new ChangeLoanClassificationCommandHandler(_loanRepository.Object, _customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingLoan_ChangesClassificationAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new ChangeLoanClassificationCommand(loan.Id.ToString(), "BadLoan", "admin");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("badloan", result.Classification);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownClassificationString_ThrowsDomainException()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new ChangeLoanClassificationCommand(loan.Id.ToString(), "NotARealClassification", "admin");

        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new ChangeLoanClassificationCommand(LoanId.New().ToString(), "WatchList", "admin");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
