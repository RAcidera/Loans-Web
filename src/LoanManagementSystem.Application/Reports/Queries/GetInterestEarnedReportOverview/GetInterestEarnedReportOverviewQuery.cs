using LoanManagementSystem.Application.Common.DTOs;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedReportOverview;

/// <summary>Same filters as the grid, no paging — the six summary cards (spec §13) plus the grid's footer totals (spec §16), computed over every record matching the filters.</summary>
public sealed record GetInterestEarnedReportOverviewQuery(
    DateOnly FromDate, DateOnly ToDate, string? Search, string? Status, string? Classification, string? InterestType
) : IRequest<InterestEarnedOverviewDto>;

public sealed class GetInterestEarnedReportOverviewQueryHandler : IRequestHandler<GetInterestEarnedReportOverviewQuery, InterestEarnedOverviewDto>
{
    private readonly InterestEarnedReportDataProvider _dataProvider;

    public GetInterestEarnedReportOverviewQueryHandler(InterestEarnedReportDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<InterestEarnedOverviewDto> Handle(GetInterestEarnedReportOverviewQuery request, CancellationToken ct)
    {
        var (loans, customerNames) = await _dataProvider.LoadFilteredLoansAsync(request.Search, request.Status, request.Classification, ct);
        var rows = _dataProvider.BuildRows(loans, customerNames, request.FromDate, request.ToDate, request.InterestType);

        var totals = new InterestEarnedTotalsDto(
            Principal: rows.Sum(r => r.Principal),
            ContractInterest: rows.Sum(r => r.ContractInterest),
            ExtensionInterest: rows.Sum(r => r.ExtensionInterest),
            EarnedBeforePeriod: rows.Sum(r => r.EarnedBeforePeriod),
            EarnedThisPeriod: rows.Sum(r => r.EarnedThisPeriod),
            TotalEarned: rows.Sum(r => r.TotalEarned),
            Adjustment: rows.Sum(r => r.Adjustment),
            FinalEarned: rows.Sum(r => r.FinalEarned),
            Count: rows.Count
        );

        var originalEarned = rows.Sum(r => r.OriginalEarnedThisPeriod);
        var extensionEarned = rows.Sum(r => r.ExtensionEarnedThisPeriod);
        var adjustments = rows.Sum(r => r.Adjustment);

        // Not "+ adjustments": originalEarned/extensionEarned are already derived from InterestCalculationService's
        // adjusted-cap accrual (see InterestEarnedReportDataProvider.BuildRow's FinalEarned comment for the same
        // reasoning) — adding the flat, non-period-scoped Adjustment again here would double-count it, and would
        // do so repeatedly across every date range a loan's Adjustment happens to appear in. InterestAdjustments
        // stays its own honest audit figure below, not folded into the primary KPI.
        var summary = new InterestEarnedSummaryDto(
            TotalEarnedInterest: originalEarned + extensionEarned,
            OriginalInterestEarned: originalEarned,
            ExtensionInterestEarned: extensionEarned,
            InterestAdjustments: adjustments,
            InterestCollected: null,
            InterestReceivable: null
        );

        return new InterestEarnedOverviewDto(summary, totals);
    }
}
