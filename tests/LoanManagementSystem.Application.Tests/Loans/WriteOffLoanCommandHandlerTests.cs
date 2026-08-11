using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.WriteOffLoan;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class WriteOffLoanCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly WriteOffLoanCommandHandler _handler;

    public WriteOffLoanCommandHandlerTests()
    {
        _handler = new WriteOffLoanCommandHandler(_loanRepository.Object, _customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingLoan_WritesOffAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _handler.Handle(new WriteOffLoanCommand(loan.Id.ToString(), "admin"), CancellationToken.None);

        Assert.Equal("writtenoff", result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FullyPaidLoan_ThrowsDomainException()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(new WriteOffLoanCommand(loan.Id.ToString(), "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new WriteOffLoanCommand(LoanId.New().ToString(), "admin"), CancellationToken.None));
    }
}
