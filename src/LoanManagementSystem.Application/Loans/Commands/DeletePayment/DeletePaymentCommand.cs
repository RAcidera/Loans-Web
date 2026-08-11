using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.DeletePayment;

/// <summary>Returns the updated LoanDto (not a PaymentDto — the payment no longer exists) so the caller sees the new Balance/Status immediately.</summary>
public sealed record DeletePaymentCommand(string LoanId, string PaymentId) : IRequest<LoanDto>;

public sealed class DeletePaymentCommandHandler : IRequestHandler<DeletePaymentCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(DeletePaymentCommand request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        loan.DeletePayment(PaymentId.Parse(request.PaymentId));
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        return loan.ToDto(customer?.FullName ?? "Unknown");
    }
}
