using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.DeleteLoanDocument;

public sealed record DeleteLoanDocumentCommand(string LoanId, string DocumentId) : IRequest;

public sealed class DeleteLoanDocumentCommandHandler : IRequestHandler<DeleteLoanDocumentCommand>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLoanDocumentCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteLoanDocumentCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdWithDocumentsAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        loan.DeleteDocument(LoanDocumentId.Parse(request.DocumentId));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
