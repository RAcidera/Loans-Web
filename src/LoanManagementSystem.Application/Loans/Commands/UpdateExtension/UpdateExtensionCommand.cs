using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.UpdateExtension;

public sealed record UpdateExtensionCommand(
    string LoanId,
    string ExtensionId,
    int ExtensionDays,
    decimal AdditionalInterestAmount,
    string? Remarks,
    decimal AdditionalChargesAmount = 0,
    string? ExtensionDate = null
) : IRequest<LoanExtensionDto>;

public sealed class UpdateExtensionCommandHandler : IRequestHandler<UpdateExtensionCommand, LoanExtensionDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExtensionCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanExtensionDto> Handle(UpdateExtensionCommand request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var extensionId = LoanExtensionId.Parse(request.ExtensionId);
        var existing = loan.Extensions.FirstOrDefault(e => e.Id == extensionId)
            ?? throw new NotFoundException(nameof(LoanExtension), request.ExtensionId);

        var extension = loan.EditExtension(
            extensionId,
            request.ExtensionDays,
            Money.Of(request.AdditionalInterestAmount),
            Money.Of(request.AdditionalChargesAmount),
            request.Remarks ?? string.Empty,
            request.ExtensionDate is not null ? DateOnly.Parse(request.ExtensionDate) : existing.ExtensionDate);

        await _unitOfWork.SaveChangesAsync(ct);

        return extension.ToDto();
    }
}
