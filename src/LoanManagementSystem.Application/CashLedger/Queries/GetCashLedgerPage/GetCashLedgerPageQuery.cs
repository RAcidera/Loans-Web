using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashLedgerPage;

/// <summary>Server-side paging + filtering for the Cash Transactions grid. TransactionType/DateFrom/DateTo are the wire strings straight off the filter bar, parsed here.</summary>
public sealed record GetCashLedgerPageQuery(
    int PageIndex, int PageSize, string? Search, string? TransactionType, string? DateFrom, string? DateTo
) : IRequest<PagedResult<CashLedgerEntryDto>>;

public sealed class GetCashLedgerPageQueryHandler : IRequestHandler<GetCashLedgerPageQuery, PagedResult<CashLedgerEntryDto>>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;

    public GetCashLedgerPageQueryHandler(ICashLedgerRepository cashLedgerRepository)
    {
        _cashLedgerRepository = cashLedgerRepository;
    }

    public async Task<PagedResult<CashLedgerEntryDto>> Handle(GetCashLedgerPageQuery request, CancellationToken ct)
    {
        var all = await _cashLedgerRepository.GetAllAsync(ct);

        // Running balance reflects the REAL cash position at that point in
        // time, so it's computed over the full, unfiltered ledger in
        // chronological order before any search/type/date filter is
        // applied — a filtered view still shows what the account balance
        // actually was on each visible row, not a balance that pretends
        // the hidden rows never happened.
        var runningBalance = new Dictionary<CashLedgerEntryId, decimal>();
        var balance = 0m;
        foreach (var entry in all.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc))
        {
            balance += entry.SignedAmount;
            runningBalance[entry.Id] = balance;
        }

        var filtered = CashLedgerFilterHelper.Apply(all, request.Search, request.TransactionType, request.DateFrom, request.DateTo)
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToList();

        var totalCount = filtered.Count;
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);

        var items = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(e => e.ToDto(runningBalance[e.Id]))
            .ToList();

        return new PagedResult<CashLedgerEntryDto>(items, totalCount);
    }
}
