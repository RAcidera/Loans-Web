using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.ChangeLoanClassification;

public sealed record ChangeLoanClassificationCommand(string LoanId, string Classification, string ChangedBy) : IRequest<LoanDto>;

public sealed class ChangeLoanClassificationCommandHandler : IRequestHandler<ChangeLoanClassificationCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeLoanClassificationCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(ChangeLoanClassificationCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        if (!Enum.TryParse<LoanClassification>(request.Classification, ignoreCase: true, out var classification))
            throw new DomainException($"Unknown loan classification '{request.Classification}'.");

        loan.ChangeClassification(classification, request.ChangedBy);
        await _unitOfWork.SaveChangesAsync(ct);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        return loan.ToDto(customer?.FullName ?? "Unknown");
    }
}
