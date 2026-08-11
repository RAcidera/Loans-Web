using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.WriteOffLoan;

public sealed record WriteOffLoanCommand(string LoanId, string WrittenOffBy) : IRequest<LoanDto>;

public sealed class WriteOffLoanCommandHandler : IRequestHandler<WriteOffLoanCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WriteOffLoanCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(WriteOffLoanCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        loan.WriteOff(request.WrittenOffBy);
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        return loan.ToDto(customer?.FullName ?? "Unknown");
    }
}
