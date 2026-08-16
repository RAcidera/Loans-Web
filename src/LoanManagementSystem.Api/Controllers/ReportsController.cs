using System.Text;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Application.Reports.Queries.ExportInterestEarnedReportXlsx;
using LoanManagementSystem.Application.Reports.Queries.ExportPeriodReportCsv;
using LoanManagementSystem.Application.Reports.Queries.GenerateInterestEarnedReportPdf;
using LoanManagementSystem.Application.Reports.Queries.GetCustomerSummary;
using LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedLoanBreakdown;
using LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedMonthlyChart;
using LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedReportOverview;
using LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedReportPage;
using LoanManagementSystem.Application.Reports.Queries.GetInterestSummary;
using LoanManagementSystem.Application.Reports.Queries.GetPeriodSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

/// <summary>SRS 3.5 "Reports" — read-only aggregation queries, available to any authenticated user.</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/reports/interest-summary — total interest earned across all non-overdue-only loans.</summary>
    [HttpGet("interest-summary")]
    public async Task<ActionResult<InterestSummaryDto>> GetInterestSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInterestSummaryQuery(), ct));

    /// <summary>GET /api/reports/customer-summary — total borrowed/paid and loan count, one row per customer.</summary>
    [HttpGet("customer-summary")]
    public async Task<ActionResult<List<CustomerSummaryDto>>> GetCustomerSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCustomerSummaryQuery(), ct));

    /// <summary>GET /api/reports/period-summary?start=&end= — loans originated, payments collected, extensions granted, interest earned, filtered to the range.</summary>
    [HttpGet("period-summary")]
    public async Task<ActionResult<PeriodSummaryDto>> GetPeriodSummary(DateOnly start, DateOnly end, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetPeriodSummaryQuery(start, end), ct));

    /// <summary>GET /api/reports/export?format=csv&start=&end= — the period view as a downloadable CSV. Only "csv" is supported today; PDF is a follow-up if actually requested.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(string format, DateOnly start, DateOnly end, CancellationToken ct)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only format=csv is supported." });

        var csv = await _mediator.Send(new ExportPeriodReportCsvQuery(start, end), ct);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"report_{start:yyyy-MM-dd}_{end:yyyy-MM-dd}.csv");
    }

    /// <summary>GET /api/reports/interest-earned/page — server-side paged, filtered, sorted detailed grid for the Interest Earned Report.</summary>
    [HttpGet("interest-earned/page")]
    public async Task<ActionResult<PagedResult<InterestEarnedRowDto>>> GetInterestEarnedPage(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? classification = null,
        [FromQuery] string? interestType = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetInterestEarnedReportPageQuery(
            fromDate, toDate, pageIndex, pageSize, search, status, classification, interestType, sortBy, sortDir), ct));

    /// <summary>GET /api/reports/interest-earned/overview — same filters as the page endpoint, no paging: the six summary cards plus the grid's footer totals.</summary>
    [HttpGet("interest-earned/overview")]
    public async Task<ActionResult<InterestEarnedOverviewDto>> GetInterestEarnedOverview(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string? search = null,
        [FromQuery] string? status = null, [FromQuery] string? classification = null, [FromQuery] string? interestType = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetInterestEarnedReportOverviewQuery(fromDate, toDate, search, status, classification, interestType), ct));

    /// <summary>GET /api/reports/interest-earned/monthly-chart — current year vs previous year, same non-date filters as the grid.</summary>
    [HttpGet("interest-earned/monthly-chart")]
    public async Task<ActionResult<List<InterestEarnedMonthlyPointDto>>> GetInterestEarnedMonthlyChart(
        [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? classification = null,
        [FromQuery] string? interestType = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetInterestEarnedMonthlyChartQuery(search, status, classification, interestType), ct));

    /// <summary>GET /api/reports/interest-earned/{loanId}/breakdown — the Loan Interest Drill-Down: every earning period (original + each extension) for one loan.</summary>
    [HttpGet("interest-earned/{loanId}/breakdown")]
    public async Task<ActionResult<InterestEarnedLoanBreakdownDto>> GetInterestEarnedLoanBreakdown(
        string loanId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInterestEarnedLoanBreakdownQuery(loanId, fromDate, toDate), ct));

    /// <summary>GET /api/reports/interest-earned/export/xlsx — same filters as the page endpoint, no paging.</summary>
    [HttpGet("interest-earned/export/xlsx")]
    public async Task<IActionResult> ExportInterestEarnedXlsx(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string? search = null,
        [FromQuery] string? status = null, [FromQuery] string? classification = null, [FromQuery] string? interestType = null,
        CancellationToken ct = default)
    {
        var file = await _mediator.Send(new ExportInterestEarnedReportXlsxQuery(fromDate, toDate, search, status, classification, interestType), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>GET /api/reports/interest-earned/export/pdf — same filters as the page endpoint, no paging.</summary>
    [HttpGet("interest-earned/export/pdf")]
    public async Task<IActionResult> ExportInterestEarnedPdf(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string? search = null,
        [FromQuery] string? status = null, [FromQuery] string? classification = null, [FromQuery] string? interestType = null,
        CancellationToken ct = default)
    {
        var file = await _mediator.Send(new GenerateInterestEarnedReportPdfQuery(fromDate, toDate, search, status, classification, interestType), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }
}
