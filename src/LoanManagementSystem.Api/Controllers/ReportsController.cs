using System.Text;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Reports.Queries.ExportPeriodReportCsv;
using LoanManagementSystem.Application.Reports.Queries.GetCustomerSummary;
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
}
