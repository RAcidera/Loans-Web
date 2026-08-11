using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Customers.Commands.UploadCustomerDocument;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Customers;

public class UploadCustomerDocumentCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UploadCustomerDocumentCommandHandler _handler;

    public UploadCustomerDocumentCommandHandlerTests()
    {
        _handler = new UploadCustomerDocumentCommandHandler(_customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_UploadsAndSaves()
    {
        var customer = Customer.Create("Maria Santos", "addr", "contact", "Fish vendor");
        _customerRepository.Setup(r => r.GetByIdWithDocumentsAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var command = new UploadCustomerDocumentCommand(customer.Id.ToString(), "id.jpg", "image/jpeg", new byte[] { 1, 2, 3 }, "admin");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("id.jpg", result.OriginalFileName);
        Assert.Equal(3, result.FileSizeBytes);
        Assert.Equal("admin", result.UploadedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownCustomerId_ThrowsNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdWithDocumentsAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new UploadCustomerDocumentCommand(CustomerId.New().ToString(), "id.jpg", "image/jpeg", new byte[] { 1 }, "admin");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisallowedContentType_ThrowsDomainException()
    {
        var customer = Customer.Create("Maria Santos", "addr", "contact", "Fish vendor");
        _customerRepository.Setup(r => r.GetByIdWithDocumentsAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var command = new UploadCustomerDocumentCommand(customer.Id.ToString(), "resume.docx", "application/msword", new byte[] { 1 }, "admin");

        await Assert.ThrowsAsync<Domain.Common.DomainException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
