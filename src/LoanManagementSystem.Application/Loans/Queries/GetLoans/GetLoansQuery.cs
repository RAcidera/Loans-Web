using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoans;

public sealed record GetLoansQuery : IRequest<List<LoanDto>>;

public sealed class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, List<LoanDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetLoansQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _appDateTime = appDateTime;
    }

    public async Task<List<LoanDto>> Handle(GetLoansQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllAsync(ct);
        var customers = (await _customerRepository.GetAllAsync(ct)).ToDictionary(c => c.Id);
        var today = _appDateTime.Today;

        return loans
            .Select(loan =>
            {
                loan.RefreshOverdueStatus(today);
                var customerName = customers.TryGetValue(loan.CustomerId, out var c) ? c.FullName : "Unknown";
                return loan.ToDto(customerName);
            })
            .ToList();
    }
}
