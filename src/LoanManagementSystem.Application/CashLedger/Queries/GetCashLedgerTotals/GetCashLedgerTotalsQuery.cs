using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashLedgerTotals;

/// <summary>Same filters as GetCashLedgerPageQuery, no paging — backs the Cash Transactions grid's "TOTALS (This Period)" / "Net Cash Movement" footer, which reflects the filtered set, not the whole ledger.</summary>
public sealed record GetCashLedgerTotalsQuery(string? Search, string? TransactionType, string? DateFrom, string? DateTo) : IRequest<CashLedgerTotalsDto>;

public sealed class GetCashLedgerTotalsQueryHandler : IRequestHandler<GetCashLedgerTotalsQuery, CashLedgerTotalsDto>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;

    public GetCashLedgerTotalsQueryHandler(ICashLedgerRepository cashLedgerRepository)
    {
        _cashLedgerRepository = cashLedgerRepository;
    }

    public async Task<CashLedgerTotalsDto> Handle(GetCashLedgerTotalsQuery request, CancellationToken ct)
    {
        var all = await _cashLedgerRepository.GetAllAsync(ct);
        var filtered = CashLedgerFilterHelper.Apply(all, request.Search, request.TransactionType, request.DateFrom, request.DateTo).ToList();

        var cashIn = filtered.Where(e => e.IsCashIn).Sum(e => e.Amount.Amount);
        var cashOut = filtered.Where(e => !e.IsCashIn).Sum(e => e.Amount.Amount);

        return new CashLedgerTotalsDto(cashIn, cashOut, cashIn - cashOut, filtered.Count);
    }
}
