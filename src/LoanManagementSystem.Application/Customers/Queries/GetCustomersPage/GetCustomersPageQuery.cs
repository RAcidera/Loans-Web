using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;

/// <summary>
/// Status is the wire-string form ("active"/"inactive"), matching CustomerDto's own casing.
/// SortBy: fullName (default), code, contact, borrowerType, status, createdAt — sorted
/// in the database — or loanCount, outstandingBalance, overdueCount, lastPaymentDate —
/// loan-derived figures, sorted across the whole filtered set before paging.
/// </summary>
public sealed record GetCustomersPageQuery(int PageIndex, int PageSize, string? Search, string? SortBy, string? SortDir, string? Status = null)
    : IRequest<PagedResult<CustomerListItemDto>>;

public sealed class GetCustomersPageQueryHandler
    : IRequestHandler<GetCustomersPageQuery, PagedResult<CustomerListItemDto>>
{
    private static readonly HashSet<string> LoanFigureSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "loanCount", "outstandingBalance", "overdueCount", "lastPaymentDate",
    };

    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetCustomersPageQueryHandler(ICustomerRepository customerRepository, ILoanRepository loanRepository, IAppDateTimeService appDateTime)
    {
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _appDateTime = appDateTime;
    }

    public async Task<PagedResult<CustomerListItemDto>> Handle(GetCustomersPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);
        var status = ParseStatus(request.Status);
        var today = _appDateTime.Today;

        if (request.SortBy is { } sortBy && LoanFigureSorts.Contains(sortBy))
            return await HandleLoanFigureSortAsync(request, sortBy, pageIndex, pageSize, status, today, ct);

        var (customers, totalCount) = await _customerRepository.GetPageAsync(
            pageIndex, pageSize, request.Search, request.SortBy, request.SortDir, status, ct);

        // Scoped to just this page's customer IDs — not a full Loans table
        // scan — so page 1 stays cheap regardless of how many loans exist.
        var stats = await _loanRepository.GetCustomerLoanStatsAsync(customers.Select(c => c.Id).ToList(), today, ct);

        return new PagedResult<CustomerListItemDto>(customers.Select(c => ToRow(c, stats)).ToList(), totalCount);
    }

    /// <summary>
    /// Sorting by a loan-derived figure: these live on Loans/Payments, not the
    /// Customers table, so the whole filtered set's ids are ordered in memory
    /// by that figure (ties by name), then only the requested page is loaded.
    /// </summary>
    private async Task<PagedResult<CustomerListItemDto>> HandleLoanFigureSortAsync(
        GetCustomersPageQuery request, string sortBy, int pageIndex, int pageSize,
        CustomerStatus? status, DateOnly today, CancellationToken ct)
    {
        var filtered = await _customerRepository.GetFilteredIdsAsync(request.Search, status, ct);
        var allStats = await _loanRepository.GetCustomerLoanStatsAsync(null, today, ct);
        var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        CustomerLoanStats StatsOf(CustomerId id) => allStats.GetValueOrDefault(id);

        IOrderedEnumerable<(CustomerId Id, string FullName)> ordered = sortBy.ToLowerInvariant() switch
        {
            "loancount" => Order(filtered, x => StatsOf(x.Id).LoanCount, desc),
            "outstandingbalance" => Order(filtered, x => StatsOf(x.Id).OutstandingBalance, desc),
            "overduecount" => Order(filtered, x => StatsOf(x.Id).OverdueLoanCount, desc),
            // Never-paid customers sort as the oldest.
            _ => Order(filtered, x => StatsOf(x.Id).LastPaymentDate ?? DateOnly.MinValue, desc),
        };

        var pageIds = ordered
            .ThenBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToList();

        var byId = (await _customerRepository.GetByIdsAsync(pageIds, ct)).ToDictionary(c => c.Id);
        var rows = pageIds
            .Where(byId.ContainsKey)
            .Select(id => ToRow(byId[id], allStats))
            .ToList();

        return new PagedResult<CustomerListItemDto>(rows, filtered.Count);
    }

    private static IOrderedEnumerable<T> Order<T, TKey>(IEnumerable<T> source, Func<T, TKey> key, bool desc) =>
        desc ? source.OrderByDescending(key) : source.OrderBy(key);

    private static CustomerListItemDto ToRow(Customer customer, IReadOnlyDictionary<CustomerId, CustomerLoanStats> stats)
    {
        var s = stats.GetValueOrDefault(customer.Id);
        return customer.ToListItemDto(s.LoanCount, s.OutstandingBalance, s.OverdueLoanCount, s.LastPaymentDate);
    }

    internal static CustomerStatus? ParseStatus(string? status) =>
        Enum.TryParse<CustomerStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;
}
