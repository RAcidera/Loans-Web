using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Financial;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashSummary;

public sealed record GetCashSummaryQuery : IRequest<CashSummaryDto>;

/// <summary>
/// The Cash Transactions page's top summary card: current Cash on Hand
/// (Formulas 1-2: Total_Cash_In - Total_Cash_Out over the WHOLE ledger),
/// plus This Month's Cash In/Cash Out/Net Change as secondary context, each
/// compared against the same figures for last calendar month. Also exposes
/// Gross Receivables so the page can display Total Business Position
/// (Gross Receivables + Cash on Hand) without re-deriving the formula.
/// </summary>
public sealed class GetCashSummaryQueryHandler : IRequestHandler<GetCashSummaryQuery, CashSummaryDto>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetCashSummaryQueryHandler(ICashLedgerRepository cashLedgerRepository, ILoanRepository loanRepository, IAppDateTimeService appDateTime)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanRepository = loanRepository;
        _appDateTime = appDateTime;
    }

    public async Task<CashSummaryDto> Handle(GetCashSummaryQuery request, CancellationToken ct)
    {
        var entries = await _cashLedgerRepository.GetAllAsync(ct);
        var cashOnHand = FinancialCalculations.ComputeCashOnHand(entries);

        var today = _appDateTime.Today;

        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);
        var (grossReceivables, _, _) = FinancialCalculations.ComputeReceivables(loans);

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
            NetChangePercent: PercentChange(netChangeThisMonth, netChangeLastMonth),
            GrossReceivables: grossReceivables
        );
    }

    private static decimal? PercentChange(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0m : null;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 1);
    }
}
