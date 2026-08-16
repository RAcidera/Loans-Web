using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomersTotals;

/// <summary>Same search/status filters as GetCustomersPageQuery, minus paging — backs the Customers list's KPI strip.</summary>
public sealed record GetCustomersTotalsQuery(string? Search = null, string? Status = null) : IRequest<CustomerTotalsDto>;

public sealed class GetCustomersTotalsQueryHandler : IRequestHandler<GetCustomersTotalsQuery, CustomerTotalsDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetCustomersTotalsQueryHandler(ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<CustomerTotalsDto> Handle(GetCustomersTotalsQuery request, CancellationToken ct)
    {
        var status = GetCustomersPageQueryHandler.ParseStatus(request.Status);
        var customers = await _customerRepository.GetFilteredAsync(request.Search, status, ct);

        var loanStats = await _loanRepository.GetLoanCountsAndBalanceByCustomerAsync(
            customers.Select(c => c.Id).ToList(), ct);

        return new CustomerTotalsDto(
            TotalCustomersCount: customers.Count,
            ActiveCustomersCount: customers.Count(c => c.Status == CustomerStatus.Active),
            InactiveCustomersCount: customers.Count(c => c.Status == CustomerStatus.Inactive),
            TotalLoansCount: loanStats.Values.Sum(v => v.LoanCount),
            TotalOutstandingBalance: loanStats.Values.Sum(v => v.OutstandingBalance)
        );
    }
}
