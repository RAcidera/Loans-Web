using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashSummary;

public sealed record GetCashSummaryQuery : IRequest<CashSummaryDto>;

/// <summary>
/// Implements the SRS's Formulas 1-5 directly off the ledger:
///   Total_Cash_In  = Σ(amount) WHERE type IN (payment_received, owner_deposit)
///   Total_Cash_Out = Σ(amount) WHERE type IN (loan_release, owner_withdrawal, expense)
///   Cash_On_Hand   = Total_Cash_In − Total_Cash_Out
///   Revolving_Funds = Cash_On_Hand   (cash-based, not receivables-based)
///   Outstanding_Principal = Σ(principal - principal_paid) for active/extended/overdue loans
/// </summary>
public sealed class GetCashSummaryQueryHandler : IRequestHandler<GetCashSummaryQuery, CashSummaryDto>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetCashSummaryQueryHandler(ICashLedgerRepository cashLedgerRepository, ILoanRepository loanRepository)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<CashSummaryDto> Handle(GetCashSummaryQuery request, CancellationToken ct)
    {
        var entries = await _cashLedgerRepository.GetAllAsync(ct);

        var totalCashIn = entries.Where(e => e.IsCashIn).Sum(e => e.Amount.Amount);
        var totalCashOut = entries.Where(e => !e.IsCashIn).Sum(e => e.Amount.Amount);
        var cashOnHand = totalCashIn - totalCashOut;

        var loans = await _loanRepository.GetAllAsync(ct);
        var outstandingPrincipal = loans
            .Where(l => l.Status is LoanStatus.Active or LoanStatus.Extended or LoanStatus.Overdue)
            .Sum(l => l.Balance.Amount);

        var sevenDayTrend = BuildSevenDayTrend(entries);

        return new CashSummaryDto(
            TotalCashIn: totalCashIn,
            TotalCashOut: totalCashOut,
            CashOnHand: cashOnHand,
            RevolvingFunds: cashOnHand,
            OutstandingPrincipal: outstandingPrincipal,
            SevenDayTrend: sevenDayTrend
        );
    }

    /// <summary>
    /// A day-by-day running Cash on Hand for the last 7 days, normalized to
    /// 0-1 for the frontend's sparkline. This is a presentation aid, not
    /// part of the SRS's own formulas.
    /// </summary>
    private static List<decimal> BuildSevenDayTrend(List<CashLedgerEntry> entries)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var runningTotals = new List<decimal>();

        for (var i = 6; i >= 0; i--)
        {
            var asOf = today.AddDays(-i);
            var total = entries.Where(e => e.TransactionDate <= asOf).Sum(e => e.SignedAmount);
            runningTotals.Add(total);
        }

        var max = runningTotals.Count > 0 ? runningTotals.Max() : 0;
        var min = runningTotals.Count > 0 ? runningTotals.Min() : 0;
        var range = max - min;

        if (range <= 0)
            return runningTotals.Select(_ => 0.5m).ToList();

        return runningTotals.Select(v => Math.Round((v - min) / range, 2)).ToList();
    }
}
