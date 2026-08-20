using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.RecordPayment;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class RecordPaymentCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAppDateTimeService> _appDateTime = new();
    private readonly RecordPaymentCommandHandler _handler;

    public RecordPaymentCommandHandlerTests()
    {
        _appDateTime.Setup(s => s.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        _handler = new RecordPaymentCommandHandler(_loanRepository.Object, _unitOfWork.Object, _appDateTime.Object);
    }

    [Fact]
    public async Task Handle_ExistingLoan_RecordsPaymentAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new RecordPaymentCommand(loan.Id.ToString(), 500, "cash", "");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(500m, result.AmountPaid);
        Assert.Equal("cash", result.PaymentMethod);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new RecordPaymentCommand(LoanId.New().ToString(), 100, "cash", "");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LoanAlreadyPaid_PropagatesDomainException_WithoutSaving()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1));
        loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 1, 5));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new RecordPaymentCommand(loan.Id.ToString(), 50, "cash", "");

        await Assert.ThrowsAsync<Domain.Common.DomainException>(() => _handler.Handle(command, CancellationToken.None));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
