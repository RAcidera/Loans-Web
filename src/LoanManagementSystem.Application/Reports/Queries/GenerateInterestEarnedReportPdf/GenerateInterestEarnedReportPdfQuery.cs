using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Application.Common.Xlsx;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GenerateInterestEarnedReportPdf;

/// <summary>Same filters as the grid, no paging — spec §26. Reuses InterestEarnedReportDataProvider so the PDF is guaranteed to show the same figures as the screen report, never recalculated independently.</summary>
public sealed record GenerateInterestEarnedReportPdfQuery(
    DateOnly FromDate, DateOnly ToDate, string? Search, string? Status, string? Classification, string? InterestType
) : IRequest<DocumentFileDto>;

public sealed class GenerateInterestEarnedReportPdfQueryHandler : IRequestHandler<GenerateInterestEarnedReportPdfQuery, DocumentFileDto>
{
    private readonly InterestEarnedReportDataProvider _dataProvider;
    private readonly IInterestEarnedReportPdfGenerator _pdfGenerator;

    public GenerateInterestEarnedReportPdfQueryHandler(InterestEarnedReportDataProvider dataProvider, IInterestEarnedReportPdfGenerator pdfGenerator)
    {
        _dataProvider = dataProvider;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<DocumentFileDto> Handle(GenerateInterestEarnedReportPdfQuery request, CancellationToken ct)
    {
        var (loans, customerNames) = await _dataProvider.LoadFilteredLoansAsync(request.Search, request.Status, request.Classification, ct);
        var rows = _dataProvider.BuildRows(loans, customerNames, request.FromDate, request.ToDate, request.InterestType)
            .OrderByDescending(r => r.LoanDate)
            .ToList();

        var totals = new InterestEarnedTotalsDto(
            Principal: rows.Sum(r => r.Principal), ContractInterest: rows.Sum(r => r.ContractInterest),
            ExtensionInterest: rows.Sum(r => r.ExtensionInterest), EarnedBeforePeriod: rows.Sum(r => r.EarnedBeforePeriod),
            EarnedThisPeriod: rows.Sum(r => r.EarnedThisPeriod), TotalEarned: rows.Sum(r => r.TotalEarned),
            Adjustment: rows.Sum(r => r.Adjustment), FinalEarned: rows.Sum(r => r.FinalEarned), Count: rows.Count);

        // Not "+ Sum(Adjustment)" - see GetInterestEarnedReportOverviewQuery's identical comment; the PDF must
        // use the same calculation as the screen report (spec §26), including this fix.
        var summary = new InterestEarnedSummaryDto(
            TotalEarnedInterest: rows.Sum(r => r.OriginalEarnedThisPeriod) + rows.Sum(r => r.ExtensionEarnedThisPeriod),
            OriginalInterestEarned: rows.Sum(r => r.OriginalEarnedThisPeriod),
            ExtensionInterestEarned: rows.Sum(r => r.ExtensionEarnedThisPeriod),
            InterestAdjustments: rows.Sum(r => r.Adjustment),
            InterestCollected: null,
            InterestReceivable: null);

        var exportRows = rows.Select(r => new InterestEarnedExportRowDto(
            LoanNumber: r.LoanNumber, CustomerName: r.CustomerName, LoanDate: r.LoanDate, DueDate: r.DueDate,
            Principal: r.Principal, ContractInterest: r.ContractInterest, ExtensionInterest: r.ExtensionInterest,
            EarnedBeforePeriod: r.EarnedBeforePeriod, EarnedThisPeriod: r.EarnedThisPeriod, TotalEarned: r.TotalEarned,
            Adjustment: r.Adjustment, FinalEarned: r.FinalEarned, Status: StatusLabel(r.Status), Classification: ClassificationLabel(r.Classification)))
            .ToList();

        var pdfDto = new InterestEarnedReportPdfDto(
            FromDate: request.FromDate.ToString("yyyy-MM-dd"),
            ToDate: request.ToDate.ToString("yyyy-MM-dd"),
            GeneratedAt: DateTime.UtcNow.ToString("MMM dd, yyyy h:mm tt") + " UTC",
            FiltersSummary: BuildFiltersSummary(request),
            Summary: summary,
            Totals: totals,
            Rows: exportRows);

        var bytes = _pdfGenerator.Generate(pdfDto);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DocumentFileDto($"interest_earned_report_{today:yyyy-MM-dd}.pdf", "application/pdf", bytes);
    }

    private static string BuildFiltersSummary(GenerateInterestEarnedReportPdfQuery request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Search)) parts.Add($"Search: \"{request.Search}\"");
        parts.Add($"Status: {(string.IsNullOrWhiteSpace(request.Status) ? "All" : StatusLabel(request.Status))}");
        parts.Add($"Classification: {(string.IsNullOrWhiteSpace(request.Classification) ? "All" : ClassificationLabel(request.Classification))}");
        parts.Add($"Interest Type: {(string.IsNullOrWhiteSpace(request.InterestType) || request.InterestType == "all" ? "All" : char.ToUpperInvariant(request.InterestType[0]) + request.InterestType[1..])}");
        return string.Join("  |  ", parts);
    }

    private static string StatusLabel(string raw) => raw switch { "writtenoff" => "Written Off", _ => char.ToUpperInvariant(raw[0]) + raw[1..] };
    private static string ClassificationLabel(string raw) => raw switch { "watchlist" => "Watch List", "badloan" => "Bad Loan", _ => char.ToUpperInvariant(raw[0]) + raw[1..] };
}
