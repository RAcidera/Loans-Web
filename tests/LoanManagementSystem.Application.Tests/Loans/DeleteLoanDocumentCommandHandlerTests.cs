using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Loans.Commands.DeleteLoanDocument;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class DeleteLoanDocumentCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteLoanDocumentCommandHandler _handler;

    public DeleteLoanDocumentCommandHandlerTests()
    {
        _handler = new DeleteLoanDocumentCommandHandler(_loanRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingDocument_DeletesAndSaves()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 1, 1));
        var document = loan.UploadDocument("agreement.pdf", "application/pdf", new byte[] { 1 }, "admin");
        _loanRepository.Setup(r => r.GetByIdWithDocumentsAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

        await _handler.Handle(new DeleteLoanDocumentCommand(loan.Id.ToString(), document.Id.ToString()), CancellationToken.None);

        Assert.Empty(loan.Documents);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdWithDocumentsAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteLoanDocumentCommand(LoanId.New().ToString(), LoanDocumentId.New().ToString()), CancellationToken.None));
    }
}
