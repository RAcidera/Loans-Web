using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.UploadLoanDocument;

/// <summary>Loan Details "Documents" tab — allowed regardless of Status; validated in LoanDocument's constructor.</summary>
public sealed record UploadLoanDocumentCommand(
    string LoanId,
    string OriginalFileName,
    string ContentType,
    byte[] Content,
    string UploadedBy
) : IRequest<LoanDocumentDto>;

public sealed class UploadLoanDocumentCommandHandler : IRequestHandler<UploadLoanDocumentCommand, LoanDocumentDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadLoanDocumentCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDocumentDto> Handle(UploadLoanDocumentCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdWithDocumentsAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var document = loan.UploadDocument(request.OriginalFileName, request.ContentType, request.Content, request.UploadedBy);
        await _unitOfWork.SaveChangesAsync(ct);

        return document.ToDto();
    }
}
