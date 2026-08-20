using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Application.Loans.Queries.ExportPaymentsXlsx;
using LoanManagementSystem.Application.Loans.Queries.GetPaymentsPage;
using LoanManagementSystem.Application.Loans.Queries.GetPaymentsTotals;
using LoanManagementSystem.Application.Loans.Queries.GetRecentPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/payments/recent?limit=6 — Angular's LoanRepository.getRecentPayments(), backs the dashboard feed.</summary>
    [HttpGet("recent")]
    public async Task<ActionResult<List<PaymentWithCustomerDto>>> GetRecent([FromQuery] int limit = 6, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetRecentPaymentsQuery(limit), ct));

    /// <summary>GET /api/payments/page?pageIndex=&amp;pageSize=&amp;loanSearch=&amp;customerSearch=&amp;dateFrom=&amp;dateTo=&amp;sortBy=&amp;sortDir=&amp;createdAtFrom=&amp;createdAtTo= — server-side paging + filtering for the standalone Payments list page.</summary>
    [HttpGet("page")]
    public async Task<ActionResult<PagedResult<PaymentWithCustomerDto>>> GetPage(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10,
        [FromQuery] string? loanSearch = null, [FromQuery] string? customerSearch = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null,
        [FromQuery] DateOnly? createdAtFrom = null, [FromQuery] DateOnly? createdAtTo = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetPaymentsPageQuery(pageIndex, pageSize, loanSearch, customerSearch, dateFrom, dateTo, sortBy, sortDir, createdAtFrom, createdAtTo), ct));

    /// <summary>GET /api/payments/page/totals — same filters as GetPage, no paging. Backs the Payments list's footer total.</summary>
    [HttpGet("page/totals")]
    public async Task<ActionResult<PaymentsTotalsDto>> GetPageTotals(
        [FromQuery] string? loanSearch = null, [FromQuery] string? customerSearch = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] DateOnly? createdAtFrom = null, [FromQuery] DateOnly? createdAtTo = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetPaymentsTotalsQuery(loanSearch, customerSearch, dateFrom, dateTo, createdAtFrom, createdAtTo), ct));

    /// <summary>GET /api/payments/export — same filters as GetPage, no paging. The Payments list's "Export" button, downloaded as .xlsx.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? loanSearch = null, [FromQuery] string? customerSearch = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null,
        [FromQuery] DateOnly? createdAtFrom = null, [FromQuery] DateOnly? createdAtTo = null, CancellationToken ct = default)
    {
        var file = await _mediator.Send(new ExportPaymentsXlsxQuery(loanSearch, customerSearch, dateFrom, dateTo, createdAtFrom, createdAtTo), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }
}
