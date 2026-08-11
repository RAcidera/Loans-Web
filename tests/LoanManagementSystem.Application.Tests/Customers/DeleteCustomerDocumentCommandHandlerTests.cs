using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Customers.Commands.DeleteCustomerDocument;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Customers;

public class DeleteCustomerDocumentCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DeleteCustomerDocumentCommandHandler _handler;

    public DeleteCustomerDocumentCommandHandlerTests()
    {
        _handler = new DeleteCustomerDocumentCommandHandler(_customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingDocument_DeletesAndSaves()
    {
        var customer = Customer.Create("Maria Santos", "addr", "contact", "Fish vendor");
        var document = customer.UploadDocument("id.jpg", "image/jpeg", new byte[] { 1 }, "admin");
        _customerRepository.Setup(r => r.GetByIdWithDocumentsAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        await _handler.Handle(new DeleteCustomerDocumentCommand(customer.Id.ToString(), document.Id.ToString()), CancellationToken.None);

        Assert.Empty(customer.Documents);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownCustomerId_ThrowsNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdWithDocumentsAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteCustomerDocumentCommand(CustomerId.New().ToString(), CustomerDocumentId.New().ToString()), CancellationToken.None));
    }
}
