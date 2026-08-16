using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Models;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedReportPage;

/// <summary>Server-side paging + filtering for the Interest Earned Report's detailed grid (spec §15/§17). Follows this codebase's 0-based pageIndex convention rather than the spec's suggested 1-based PageNumber.</summary>
public sealed record GetInterestEarnedReportPageQuery(
    DateOnly FromDate, DateOnly ToDate, int PageIndex, int PageSize,
    string? Search, string? Status, string? Classification, string? InterestType,
    string? SortBy, string? SortDir
) : IRequest<PagedResult<InterestEarnedRowDto>>;

public sealed class GetInterestEarnedReportPageQueryHandler : IRequestHandler<GetInterestEarnedReportPageQuery, PagedResult<InterestEarnedRowDto>>
{
    private readonly InterestEarnedReportDataProvider _dataProvider;

    public GetInterestEarnedReportPageQueryHandler(InterestEarnedReportDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<PagedResult<InterestEarnedRowDto>> Handle(GetInterestEarnedReportPageQuery request, CancellationToken ct)
    {
        var (loans, customerNames) = await _dataProvider.LoadFilteredLoansAsync(request.Search, request.Status, request.Classification, ct);
        var rows = _dataProvider.BuildRows(loans, customerNames, request.FromDate, request.ToDate, request.InterestType);

        var sorted = ApplySort(rows, request.SortBy, request.SortDir);

        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);
        var pageItems = sorted.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return new PagedResult<InterestEarnedRowDto>(pageItems, sorted.Count);
    }

    private static List<InterestEarnedRowDto> ApplySort(List<InterestEarnedRowDto> rows, string? sortBy, string? sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<InterestEarnedRowDto> Order<TKey>(Func<InterestEarnedRowDto, TKey> keySelector) =>
            desc ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);

        // Default: LoanDate desc — matches the spec's suggested SortColumn/SortDirection defaults.
        return (string.IsNullOrWhiteSpace(sortBy) ? rows.OrderByDescending(r => r.LoanDate) : sortBy.ToLowerInvariant() switch
        {
            "loannumber" => Order(r => r.LoanNumber),
            "customer" => Order(r => r.CustomerName),
            "loandate" => Order(r => r.LoanDate),
            "duedate" => Order(r => r.DueDate),
            "principal" => Order(r => r.Principal),
            "contractinterest" => Order(r => r.ContractInterest),
            "extensioninterest" => Order(r => r.ExtensionInterest),
            "earnedthisperiod" => Order(r => r.EarnedThisPeriod),
            "totalearned" => Order(r => r.TotalEarned),
            "finalearned" => Order(r => r.FinalEarned),
            _ => rows.OrderByDescending(r => r.LoanDate),
        }).ToList();
    }
}
