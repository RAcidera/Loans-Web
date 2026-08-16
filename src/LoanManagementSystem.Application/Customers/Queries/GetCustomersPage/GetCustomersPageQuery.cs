using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;

/// <summary>Status is the wire-string form ("active"/"inactive"), matching CustomerDto's own casing.</summary>
public sealed record GetCustomersPageQuery(int PageIndex, int PageSize, string? Search, string? SortBy, string? SortDir, string? Status = null)
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
        var status = ParseStatus(request.Status);

        var (customers, totalCount) = await _customerRepository.GetPageAsync(
            pageIndex, pageSize, request.Search, request.SortBy, request.SortDir, status, ct);

        // Scoped to just this page's customer IDs — not a full Loans table
        // scan — so page 1 stays cheap regardless of how many loans exist.
        var loanStats = await _loanRepository.GetLoanCountsAndBalanceByCustomerAsync(
            customers.Select(c => c.Id).ToList(), ct);

        var items = customers
            .Select(c =>
            {
                var (loanCount, balance) = loanStats.GetValueOrDefault(c.Id);
                return c.ToListItemDto(loanCount, balance);
            })
            .ToList();

        return new PagedResult<CustomerListItemDto>(items, totalCount);
    }

    internal static CustomerStatus? ParseStatus(string? status) =>
        Enum.TryParse<CustomerStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;
}
