using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Loans.Queries.GetLoansPage;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoansTotals;

/// <summary>Same filter set as GetLoansPageQuery, minus paging/sort — backs the Loans list's footer totals row.</summary>
public sealed record GetLoansTotalsQuery(
    string? Search = null, string? Status = null, string? Classification = null,
    DateOnly? LoanDateFrom = null, DateOnly? LoanDateTo = null,
    DateOnly? DueDateFrom = null, DateOnly? DueDateTo = null,
    bool BadLoansOnly = false, bool OverdueOnly = false)
    : IRequest<LoanTotalsDto>;

public sealed class GetLoansTotalsQueryHandler : IRequestHandler<GetLoansTotalsQuery, LoanTotalsDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetLoansTotalsQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<LoanTotalsDto> Handle(GetLoansTotalsQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var matchingCustomerIds = string.IsNullOrWhiteSpace(request.Search)
            ? new List<Domain.Customers.CustomerId>()
            : await _customerRepository.SearchIdsByNameAsync(request.Search, ct);

        // Reuses GetLoansPageQuery's own string-to-enum filter parsing so
        // the list and its footer totals can never read different filters
        // for the same request.
        var filters = GetLoansPageQueryHandler.BuildFilters(new GetLoansPageQuery(
            0, 1, request.Search, null, null, request.Status, request.Classification,
            request.LoanDateFrom, request.LoanDateTo, request.DueDateFrom, request.DueDateTo,
            request.BadLoansOnly, request.OverdueOnly));

        var loans = await _loanRepository.GetFilteredAsync(request.Search, matchingCustomerIds, filters, today, ct);

        return new LoanTotalsDto(
            TotalPrincipal: loans.Sum(l => l.PrincipalAmount.Amount),
            TotalInterest: loans.Sum(l => l.TotalInterest.Amount),
            TotalExtensionCharges: loans.Sum(l => l.TotalExtensionCharges.Amount),
            TotalPayments: loans.Sum(l => l.TotalPaid.Amount),
            TotalOutstandingBalance: loans.Sum(l => l.Balance.Amount)
        );
    }
}
