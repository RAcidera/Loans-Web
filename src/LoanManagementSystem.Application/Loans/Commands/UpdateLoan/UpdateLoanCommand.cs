using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.UpdateLoan;

/// <summary>
/// Every field is optional so a caller can override just one — spec: "the
/// user must be able to manually override Loan Date/Due Date/Interest
/// Rate/Interest Amount" post-creation, e.g. a goodwill discount before an
/// early payoff.
/// </summary>
public sealed record UpdateLoanCommand(
    string LoanId,
    string EditedBy,
    string? StartDate = null,
    string? DueDate = null,
    decimal? InterestRate = null,
    decimal? InterestAmount = null,
    string? Remarks = null
) : IRequest<LoanDto>;

public sealed class UpdateLoanCommandHandler : IRequestHandler<UpdateLoanCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLoanCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(UpdateLoanCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        loan.EditLoan(
            startDate: request.StartDate is not null ? DateOnly.Parse(request.StartDate) : null,
            dueDate: request.DueDate is not null ? DateOnly.Parse(request.DueDate) : null,
            interestRate: request.InterestRate is not null ? InterestRate.Of(request.InterestRate.Value) : null,
            interestAmount: request.InterestAmount is not null ? Money.Of(request.InterestAmount.Value) : null,
            remarks: request.Remarks,
            editedBy: request.EditedBy);

        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        return loan.ToDto(customer?.FullName ?? "Unknown");
    }
}
