using LoanManagementSystem.Application.Common.DateTimeHandling;
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
/// InterestRate and PaymentTermsMonths are optional, defaulting to the
/// SRS's stated 3% / 2 months (60 days) when not supplied, so a caller can
/// originate a standard loan with just a customer and a principal.
/// InterestAmount, when supplied, overrides the rate*principal calculation
/// outright — see Loan.Originate's doc comment.
/// </summary>
public sealed record CreateLoanCommand(
    string CustomerId,
    decimal Principal,
    decimal? InterestRate = null,
    int? PaymentTermsMonths = null,
    string? StartDate = null,
    decimal? InterestAmount = null
) : IRequest<LoanDto>;

public sealed class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateTimeService _appDateTime;

    public CreateLoanCommandHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _appDateTime = appDateTime;
    }

    public async Task<LoanDto> Handle(CreateLoanCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var startDate = request.StartDate is not null
            ? DateOnly.Parse(request.StartDate)
            : _appDateTime.Today;

        var rate = request.InterestRate is not null ? InterestRate.Of(request.InterestRate.Value) : InterestRate.Default;
        var paymentTermsMonths = request.PaymentTermsMonths ?? 2;
        var termDays = paymentTermsMonths * 30;
        var interestAmount = request.InterestAmount is not null ? Money.Of(request.InterestAmount.Value) : null;

        var loan = Loan.Originate(customerId, Money.Of(request.Principal), rate, startDate, termDays, paymentTermsMonths, interestAmount);

        _loanRepository.Add(loan);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes the LoanCreatedDomainEvent → loan_release ledger entry, and populates loan.LoanNumber from the DB

        return loan.ToDto(customer.FullName);
    }
}
