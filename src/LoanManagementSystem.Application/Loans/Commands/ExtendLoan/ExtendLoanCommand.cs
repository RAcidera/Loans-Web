using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.ExtendLoan;

public sealed record ExtendLoanCommand(
    string LoanId,
    int ExtensionDays,
    string Remarks,
    decimal AdditionalChargesAmount = 0
) : IRequest<LoanExtensionDto>;

public sealed class ExtendLoanCommandHandler : IRequestHandler<ExtendLoanCommand, LoanExtensionDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExtendLoanCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanExtensionDto> Handle(ExtendLoanCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var extension = loan.Extend(
            request.ExtensionDays,
            Money.Of(request.AdditionalChargesAmount),
            request.Remarks,
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _unitOfWork.SaveChangesAsync(ct);

        return extension.ToDto();
    }
}
