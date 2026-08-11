using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;

public sealed record GetCustomersPageQuery(int PageIndex, int PageSize, string? Search, string? SortBy, string? SortDir)
    : IRequest<PagedResult<CustomerListItemDto>>;

public sealed class GetCustomersPageQueryHandler
    : IRequestHandler<GetCustomersPageQuery, PagedResult<CustomerListItemDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetCustomersPageQueryHandler(ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<PagedResult<CustomerListItemDto>> Handle(GetCustomersPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);

        var (customers, totalCount) = await _customerRepository.GetPageAsync(
            pageIndex, pageSize, request.Search, request.SortBy, request.SortDir, ct);

        // Scoped to just this page's customer IDs — not a full Loans table
        // scan — so page 1 stays cheap regardless of how many loans exist.
        var loanCounts = await _loanRepository.GetLoanCountsByCustomerAsync(
            customers.Select(c => c.Id).ToList(), ct);

        var items = customers
            .Select(c => c.ToListItemDto(loanCounts.GetValueOrDefault(c.Id)))
            .ToList();

        return new PagedResult<CustomerListItemDto>(items, totalCount);
    }
}
