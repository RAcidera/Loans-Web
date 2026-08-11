using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.DeletePayment;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class DeletePaymentCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeletePaymentCommandHandler _handler;

    public DeletePaymentCommandHandlerTests()
    {
        _handler = new DeletePaymentCommandHandler(_loanRepository.Object, _customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingPayment_DeletesAndReturnsUpdatedLoan()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var result = await _handler.Handle(new DeletePaymentCommand(loan.Id.ToString(), payment.Id.ToString()), CancellationToken.None);

        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(1000m, result.Balance);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeletePaymentCommand(LoanId.New().ToString(), PaymentId.New().ToString()), CancellationToken.None));
    }
}
