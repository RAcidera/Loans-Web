using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Commands.UploadCustomerDocument;

/// <summary>Spec 3.1 "Customer Documents Management" — allows multiple file uploads (JPG/PNG/PDF), validated in CustomerDocument's constructor.</summary>
public sealed record UploadCustomerDocumentCommand(
    string CustomerId,
    string OriginalFileName,
    string ContentType,
    byte[] Content,
    string UploadedBy
) : IRequest<CustomerDocumentDto>;

public sealed class UploadCustomerDocumentCommandHandler : IRequestHandler<UploadCustomerDocumentCommand, CustomerDocumentDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadCustomerDocumentCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDocumentDto> Handle(UploadCustomerDocumentCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdWithDocumentsAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var document = customer.UploadDocument(request.OriginalFileName, request.ContentType, request.Content, request.UploadedBy);
        await _unitOfWork.SaveChangesAsync(ct);

        return document.ToDto();
    }
}
