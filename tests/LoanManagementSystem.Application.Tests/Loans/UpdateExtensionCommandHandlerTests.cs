using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.UpdateExtension;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class UpdateExtensionCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UpdateExtensionCommandHandler _handler;

    public UpdateExtensionCommandHandlerTests()
    {
        _handler = new UpdateExtensionCommandHandler(_loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingExtension_UpdatesAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 30);
        var extension = loan.Extend(10, Money.Of(10), "initial", new DateOnly(2026, 1, 20));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UpdateExtensionCommand(loan.Id.ToString(), extension.Id.ToString(), 20, "revised", 15);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(20, result.ExtensionDays);
        Assert.Equal(15m, result.AdditionalChargesAmount);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownExtensionId_ThrowsNotFound()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UpdateExtensionCommand(loan.Id.ToString(), LoanExtensionId.New().ToString(), 10, "x", 10);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
