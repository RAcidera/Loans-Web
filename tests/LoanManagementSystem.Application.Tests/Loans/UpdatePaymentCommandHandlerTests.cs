using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.UpdatePayment;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class UpdatePaymentCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdatePaymentCommandHandler _handler;

    public UpdatePaymentCommandHandlerTests()
    {
        _handler = new UpdatePaymentCommandHandler(_loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingPayment_UpdatesAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 1, 10));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UpdatePaymentCommand(loan.Id.ToString(), payment.Id.ToString(), 600m, "gcash", "corrected", "REF-9", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(600m, result.AmountPaid);
        Assert.Equal("gcash", result.PaymentMethod);
        Assert.Equal("REF-9", result.ReferenceNumber);
        Assert.Equal(600m, loan.TotalPaid.Amount);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownPaymentId_ThrowsNotFound()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UpdatePaymentCommand(loan.Id.ToString(), PaymentId.New().ToString(), 100m, "cash", null, null, null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new UpdatePaymentCommand(LoanId.New().ToString(), PaymentId.New().ToString(), 100m, "cash", null, null, null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
