using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.CreateLoan;

/// <summary>
/// InterestRate and TermDays are optional, defaulting to the SRS's stated
/// 3% / 60 days (3.2) when not supplied, so a caller can originate a
/// standard loan with just a customer and a principal.
/// </summary>
public sealed record CreateLoanCommand(
    string CustomerId,
    decimal Principal,
    decimal? InterestRate = null,
    int? TermDays = null,
    string? StartDate = null
) : IRequest<LoanDto>;

public sealed class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLoanCommandHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoanDto> Handle(CreateLoanCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var startDate = request.StartDate is not null
            ? DateOnly.Parse(request.StartDate)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var rate = request.InterestRate is not null ? InterestRate.Of(request.InterestRate.Value) : InterestRate.Default;
        var termDays = request.TermDays ?? 60;

        var loan = Loan.Originate(customerId, Money.Of(request.Principal), rate, startDate, termDays);

        _loanRepository.Add(loan);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes the LoanCreatedDomainEvent → loan_release ledger entry, and populates loan.LoanNumber from the DB

        return loan.ToDto(customer.FullName);
    }
}
