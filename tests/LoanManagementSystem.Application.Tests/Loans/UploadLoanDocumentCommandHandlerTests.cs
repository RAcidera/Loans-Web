using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.UploadLoanDocument;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class UploadLoanDocumentCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UploadLoanDocumentCommandHandler _handler;

    public UploadLoanDocumentCommandHandlerTests()
    {
        _handler = new UploadLoanDocumentCommandHandler(_loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingLoan_UploadsAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        _loanRepository.Setup(r => r.GetByIdWithDocumentsAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        var command = new UploadLoanDocumentCommand(loan.Id.ToString(), "agreement.pdf", "application/pdf", new byte[] { 1, 2 }, "admin");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("agreement.pdf", result.OriginalFileName);
        Assert.Equal(2, result.FileSizeBytes);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdWithDocumentsAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        var command = new UploadLoanDocumentCommand(LoanId.New().ToString(), "agreement.pdf", "application/pdf", new byte[] { 1 }, "admin");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
