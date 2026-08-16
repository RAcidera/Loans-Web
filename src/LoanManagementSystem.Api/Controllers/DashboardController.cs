using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Loans.Queries.GetDashboardReceivables;
using LoanManagementSystem.Application.Loans.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

/// <summary>Spec's "Dashboard Receivable Calculations" + "Dashboard Summary Cards".</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/dashboard/receivables — Gross/Collectible/Bad Loan Receivables plus loan status counts.</summary>
    [HttpGet("receivables")]
    public async Task<ActionResult<DashboardReceivablesDto>> GetReceivables(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDashboardReceivablesQuery(), ct));

    /// <summary>GET /api/dashboard/summary — trend badges, Collections Overview/Past-7-Days charts, Receivables Breakdown, and Recent Loans.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDashboardSummaryQuery(), ct));
}
