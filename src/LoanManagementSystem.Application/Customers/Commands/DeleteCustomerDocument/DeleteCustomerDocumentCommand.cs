using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Commands.DeleteCustomerDocument;

public sealed record DeleteCustomerDocumentCommand(string CustomerId, string DocumentId) : IRequest;

public sealed class DeleteCustomerDocumentCommandHandler : IRequestHandler<DeleteCustomerDocumentCommand>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerDocumentCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCustomerDocumentCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdWithDocumentsAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        customer.DeleteDocument(CustomerDocumentId.Parse(request.DocumentId));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
