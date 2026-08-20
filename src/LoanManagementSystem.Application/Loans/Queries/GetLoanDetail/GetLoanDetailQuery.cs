using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanDetail;

public sealed record GetLoanDetailQuery(string LoanId) : IRequest<LoanDetailDto>;

/// <summary>Composes one loan + its extensions + its payments — the same shape Angular's GetLoanDetailUseCase builds client-side via forkJoin.</summary>
public sealed class GetLoanDetailQueryHandler : IRequestHandler<GetLoanDetailQuery, LoanDetailDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetLoanDetailQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _appDateTime = appDateTime;
    }

    public async Task<LoanDetailDto> Handle(GetLoanDetailQuery request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct);
        if (loan is null)
            return new LoanDetailDto(null, new List<LoanExtensionDto>(), new List<PaymentDto>());

        var today = _appDateTime.Today;
        loan.RefreshOverdueStatus(today);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        var daysUntilDue = loan.DueDate.DayNumber - today.DayNumber;
        var loanDto = loan.ToDto(customer?.FullName ?? "Unknown", customer?.ContactNumber, daysUntilDue);

        var extensions = loan.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc).Select(e => e.ToDto()).ToList();
        var payments = loan.Payments.OrderBy(p => p.PaymentDate).Select(p => p.ToDto()).ToList();

        return new LoanDetailDto(loanDto, extensions, payments);
    }
}
