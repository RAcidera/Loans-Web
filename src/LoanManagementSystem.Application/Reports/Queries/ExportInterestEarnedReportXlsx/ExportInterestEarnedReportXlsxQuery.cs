using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Xlsx;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.ExportInterestEarnedReportXlsx;

/// <summary>Same filters as the grid, no paging — exports the whole filtered result set (spec §27).</summary>
public sealed record ExportInterestEarnedReportXlsxQuery(
    DateOnly FromDate, DateOnly ToDate, string? Search, string? Status, string? Classification, string? InterestType
) : IRequest<DocumentFileDto>;

public sealed class ExportInterestEarnedReportXlsxQueryHandler : IRequestHandler<ExportInterestEarnedReportXlsxQuery, DocumentFileDto>
{
    private readonly InterestEarnedReportDataProvider _dataProvider;
    private readonly IInterestEarnedReportXlsxExportGenerator _xlsxGenerator;
    private readonly IAppDateTimeService _appDateTime;

    public ExportInterestEarnedReportXlsxQueryHandler(
        InterestEarnedReportDataProvider dataProvider, IInterestEarnedReportXlsxExportGenerator xlsxGenerator, IAppDateTimeService appDateTime)
    {
        _dataProvider = dataProvider;
        _xlsxGenerator = xlsxGenerator;
        _appDateTime = appDateTime;
    }

    public async Task<DocumentFileDto> Handle(ExportInterestEarnedReportXlsxQuery request, CancellationToken ct)
    {
        var (loans, customerNames) = await _dataProvider.LoadFilteredLoansAsync(request.Search, request.Status, request.Classification, ct);
        var rows = _dataProvider.BuildRows(loans, customerNames, request.FromDate, request.ToDate, request.InterestType)
            .OrderByDescending(r => r.LoanDate)
            .Select(r => new InterestEarnedExportRowDto(
                LoanNumber: r.LoanNumber, CustomerName: r.CustomerName, LoanDate: r.LoanDate, DueDate: r.DueDate,
                Principal: r.Principal, ContractInterest: r.ContractInterest, ExtensionInterest: r.ExtensionInterest,
                EarnedBeforePeriod: r.EarnedBeforePeriod, EarnedThisPeriod: r.EarnedThisPeriod, TotalEarned: r.TotalEarned,
                Adjustment: r.Adjustment, FinalEarned: r.FinalEarned, Status: StatusLabel(r.Status), Classification: ClassificationLabel(r.Classification)))
            .ToList();

        var bytes = _xlsxGenerator.Generate(rows);
        var today = _appDateTime.Today;
        return new DocumentFileDto($"interest_earned_report_{today:yyyy-MM-dd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }

    private static string StatusLabel(string raw) => raw switch { "writtenoff" => "Written Off", _ => char.ToUpperInvariant(raw[0]) + raw[1..] };
    private static string ClassificationLabel(string raw) => raw switch { "watchlist" => "Watch List", "badloan" => "Bad Loan", _ => char.ToUpperInvariant(raw[0]) + raw[1..] };
}
