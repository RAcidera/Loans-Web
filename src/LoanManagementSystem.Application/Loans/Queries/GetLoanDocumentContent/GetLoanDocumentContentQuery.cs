using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanDocumentContent;

/// <summary>Backs the download endpoint — the one query allowed to carry a document's raw bytes.</summary>
public sealed record GetLoanDocumentContentQuery(string LoanId, string DocumentId) : IRequest<DocumentFileDto?>;

public sealed class GetLoanDocumentContentQueryHandler : IRequestHandler<GetLoanDocumentContentQuery, DocumentFileDto?>
{
    private readonly ILoanRepository _loanRepository;

    public GetLoanDocumentContentQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<DocumentFileDto?> Handle(GetLoanDocumentContentQuery request, CancellationToken ct)
    {
        var document = await _loanRepository.GetDocumentContentAsync(
            LoanId.Parse(request.LoanId), LoanDocumentId.Parse(request.DocumentId), ct);

        return document is null ? null : new DocumentFileDto(document.OriginalFileName, document.ContentType, document.Content);
    }
}
