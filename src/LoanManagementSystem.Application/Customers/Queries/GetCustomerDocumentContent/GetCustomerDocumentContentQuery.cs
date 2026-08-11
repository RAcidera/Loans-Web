using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomerDocumentContent;

/// <summary>Backs the download endpoint — the one query allowed to carry a document's raw bytes.</summary>
public sealed record GetCustomerDocumentContentQuery(string CustomerId, string DocumentId) : IRequest<DocumentFileDto?>;

public sealed class GetCustomerDocumentContentQueryHandler : IRequestHandler<GetCustomerDocumentContentQuery, DocumentFileDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerDocumentContentQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<DocumentFileDto?> Handle(GetCustomerDocumentContentQuery request, CancellationToken ct)
    {
        var document = await _customerRepository.GetDocumentContentAsync(
            CustomerId.Parse(request.CustomerId), CustomerDocumentId.Parse(request.DocumentId), ct);

        return document is null ? null : new DocumentFileDto(document.OriginalFileName, document.ContentType, document.Content);
    }
}
