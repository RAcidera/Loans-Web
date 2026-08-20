using System.Globalization;
using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedMonthlyChart;

/// <summary>"Earned Interest by Month" grouped bar chart (spec §14) — current year vs previous year, same non-date filters as the grid, computed by re-running the same daily-accrual engine over each of the 24 calendar months.</summary>
public sealed record GetInterestEarnedMonthlyChartQuery(
    string? Search, string? Status, string? Classification, string? InterestType
) : IRequest<List<InterestEarnedMonthlyPointDto>>;

public sealed class GetInterestEarnedMonthlyChartQueryHandler : IRequestHandler<GetInterestEarnedMonthlyChartQuery, List<InterestEarnedMonthlyPointDto>>
{
    private readonly InterestEarnedReportDataProvider _dataProvider;
    private readonly IAppDateTimeService _appDateTime;

    public GetInterestEarnedMonthlyChartQueryHandler(InterestEarnedReportDataProvider dataProvider, IAppDateTimeService appDateTime)
    {
        _dataProvider = dataProvider;
        _appDateTime = appDateTime;
    }

    public async Task<List<InterestEarnedMonthlyPointDto>> Handle(GetInterestEarnedMonthlyChartQuery request, CancellationToken ct)
    {
        var (loans, customerNames) = await _dataProvider.LoadFilteredLoansAsync(request.Search, request.Status, request.Classification, ct);

        var currentYear = _appDateTime.Today.Year;
        var previousYear = currentYear - 1;
        var points = new List<InterestEarnedMonthlyPointDto>();

        for (var month = 1; month <= 12; month++)
        {
            var currentYearEarned = EarnedInMonth(loans, customerNames, currentYear, month, request.InterestType);
            var previousYearEarned = EarnedInMonth(loans, customerNames, previousYear, month, request.InterestType);

            points.Add(new InterestEarnedMonthlyPointDto(
                Month: CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                CurrentYear: currentYearEarned,
                PreviousYear: previousYearEarned
            ));
        }

        return points;
    }

    private decimal EarnedInMonth(
        List<Domain.Loans.Loan> loans, Dictionary<Domain.Customers.CustomerId, string> customerNames, int year, int month, string? interestType)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var rows = _dataProvider.BuildRows(loans, customerNames, monthStart, monthEnd, interestType);
        return rows.Sum(r => r.EarnedThisPeriod);
    }
}
