using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.DeleteExtension;

/// <summary>Returns the updated LoanDto (not a LoanExtensionDto — the extension no longer exists) so the caller sees the reverted DueDate/Balance immediately.</summary>
public sealed record DeleteExtensionCommand(string LoanId, string ExtensionId) : IRequest<LoanDto>;

public sealed class DeleteExtensionCommandHandler : IRequestHandler<DeleteExtensionCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteExtensionCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(DeleteExtensionCommand request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        loan.DeleteExtension(LoanExtensionId.Parse(request.ExtensionId));
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        return loan.ToDto(customer?.FullName ?? "Unknown");
    }
}
