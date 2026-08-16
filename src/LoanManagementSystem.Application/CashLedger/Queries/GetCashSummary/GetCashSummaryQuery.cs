using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashSummary;

public sealed record GetCashSummaryQuery : IRequest<CashSummaryDto>;

/// <summary>
/// The Cash Transactions page's top summary card: current Cash on Hand
/// (Formulas 1-2: Total_Cash_In - Total_Cash_Out over the WHOLE ledger),
/// plus This Month's Cash In/Cash Out/Net Change as secondary context, each
/// compared against the same figures for last calendar month.
/// </summary>
public sealed class GetCashSummaryQueryHandler : IRequestHandler<GetCashSummaryQuery, CashSummaryDto>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;

    public GetCashSummaryQueryHandler(ICashLedgerRepository cashLedgerRepository)
    {
        _cashLedgerRepository = cashLedgerRepository;
    }

    public async Task<CashSummaryDto> Handle(GetCashSummaryQuery request, CancellationToken ct)
    {
        var entries = await _cashLedgerRepository.GetAllAsync(ct);
        var cashOnHand = entries.Sum(e => e.SignedAmount);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var thisMonth = entries.Where(e => e.TransactionDate >= monthStart && e.TransactionDate <= today).ToList();
        var lastMonth = entries.Where(e => e.TransactionDate >= lastMonthStart && e.TransactionDate < monthStart).ToList();

        var cashInThisMonth = thisMonth.Where(e => e.IsCashIn).Sum(e => e.Amount.Amount);
        var cashOutThisMonth = thisMonth.Where(e => !e.IsCashIn).Sum(e => e.Amount.Amount);
        var netChangeThisMonth = cashInThisMonth - cashOutThisMonth;

        var cashInLastMonth = lastMonth.Where(e => e.IsCashIn).Sum(e => e.Amount.Amount);
        var cashOutLastMonth = lastMonth.Where(e => !e.IsCashIn).Sum(e => e.Amount.Amount);
        var netChangeLastMonth = cashInLastMonth - cashOutLastMonth;

        var oneMonthAgo = today.AddMonths(-1);
        var cashOnHandOneMonthAgo = entries.Where(e => e.TransactionDate <= oneMonthAgo).Sum(e => e.SignedAmount);

        return new CashSummaryDto(
            CashOnHand: cashOnHand,
            AsOfDate: today.ToString("yyyy-MM-dd"),
            CashInThisMonth: cashInThisMonth,
            CashOutThisMonth: cashOutThisMonth,
            NetChangeThisMonth: netChangeThisMonth,
            CashOnHandChangePercent: PercentChange(cashOnHand, cashOnHandOneMonthAgo),
            CashInChangePercent: PercentChange(cashInThisMonth, cashInLastMonth),
            CashOutChangePercent: PercentChange(cashOutThisMonth, cashOutLastMonth),
            NetChangePercent: PercentChange(netChangeThisMonth, netChangeLastMonth)
        );
    }

    private static decimal? PercentChange(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0m : null;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 1);
    }
}
