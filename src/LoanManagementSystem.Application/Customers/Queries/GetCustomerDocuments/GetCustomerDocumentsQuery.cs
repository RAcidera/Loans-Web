using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomerDocuments;

/// <summary>Metadata-only list — backs the Customer profile's Documents section. See ICustomerRepository.GetDocumentsMetadataAsync for why this never touches Content.</summary>
public sealed record GetCustomerDocumentsQuery(string CustomerId) : IRequest<List<CustomerDocumentDto>>;

public sealed class GetCustomerDocumentsQueryHandler : IRequestHandler<GetCustomerDocumentsQuery, List<CustomerDocumentDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerDocumentsQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<List<CustomerDocumentDto>> Handle(GetCustomerDocumentsQuery request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var rows = await _customerRepository.GetDocumentsMetadataAsync(customerId, ct);

        return rows
            .Select(r => new CustomerDocumentDto(
                DocumentId: r.Id.ToString(),
                CustomerId: request.CustomerId,
                OriginalFileName: r.OriginalFileName,
                ContentType: r.ContentType,
                FileSizeBytes: r.FileSizeBytes,
                UploadedAt: r.UploadedAtUtc.ToString("O"),
                UploadedBy: r.UploadedBy))
            .ToList();
    }
}
