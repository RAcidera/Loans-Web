using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanDocuments;

/// <summary>Metadata-only list — backs the Loan Details "Documents" tab. See ILoanRepository.GetDocumentsMetadataAsync for why this never touches Content.</summary>
public sealed record GetLoanDocumentsQuery(string LoanId) : IRequest<List<LoanDocumentDto>>;

public sealed class GetLoanDocumentsQueryHandler : IRequestHandler<GetLoanDocumentsQuery, List<LoanDocumentDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetLoanDocumentsQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<List<LoanDocumentDto>> Handle(GetLoanDocumentsQuery request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var rows = await _loanRepository.GetDocumentsMetadataAsync(loanId, ct);

        return rows
            .Select(r => new LoanDocumentDto(
                DocumentId: r.Id.ToString(),
                LoanId: request.LoanId,
                OriginalFileName: r.OriginalFileName,
                ContentType: r.ContentType,
                FileSizeBytes: r.FileSizeBytes,
                UploadedAt: r.UploadedAtUtc.ToString("O"),
                UploadedBy: r.UploadedBy))
            .ToList();
    }
}
