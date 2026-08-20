using LoanManagementSystem.Application.Common.DateTimeHandling;
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
    private readonly IAppDateTimeService _appDateTime;

    public ExtendLoanCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
        _appDateTime = appDateTime;
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
            _appDateTime.Today);

        await _unitOfWork.SaveChangesAsync(ct);

        return extension.ToDto();
    }
}
