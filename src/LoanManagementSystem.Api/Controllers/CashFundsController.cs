using LoanManagementSystem.Application.CashLedger.Commands.AddCashTransaction;
using LoanManagementSystem.Application.CashLedger.Commands.DeleteCashTransaction;
using LoanManagementSystem.Application.CashLedger.Commands.EditCashTransaction;
using LoanManagementSystem.Application.CashLedger.Queries.ExportCashLedgerXlsx;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashLedger;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashLedgerPage;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashLedgerTotals;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashSummary;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/cash-funds")]
[Authorize]
public class CashFundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashFundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/cash-funds/summary — Angular's CashLedgerRepository.getSummary(). Cash on Hand + This Month's Cash In/Out/Net Change.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<CashSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCashSummaryQuery(), ct));

    /// <summary>GET /api/cash-funds/ledger — the full, unpaged ledger. Kept for callers that want everything at once; the Transactions page itself uses GetPage below.</summary>
    [HttpGet("ledger")]
    public async Task<ActionResult<List<CashLedgerEntryDto>>> GetLedger(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCashLedgerQuery(), ct));

    /// <summary>GET /api/cash-funds/ledger/page?pageIndex=&amp;pageSize=&amp;search=&amp;type=&amp;dateFrom=&amp;dateTo= — server-side paging + filtering for the Cash Transactions grid, each row including its running balance.</summary>
    [HttpGet("ledger/page")]
    public async Task<ActionResult<PagedResult<CashLedgerEntryDto>>> GetPage(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 15, [FromQuery] string? search = null,
        [FromQuery] string? type = null, [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCashLedgerPageQuery(pageIndex, pageSize, search, type, dateFrom, dateTo), ct));

    /// <summary>GET /api/cash-funds/ledger/page/totals — same filters as GetPage, no paging. Backs the grid's "TOTALS (This Period)" / "Net Cash Movement" footer.</summary>
    [HttpGet("ledger/page/totals")]
    public async Task<ActionResult<CashLedgerTotalsDto>> GetPageTotals(
        [FromQuery] string? search = null, [FromQuery] string? type = null,
        [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCashLedgerTotalsQuery(search, type, dateFrom, dateTo), ct));

    /// <summary>GET /api/cash-funds/ledger/export — same filters as GetPage, no paging. The Cash Transactions grid's "Export" button, downloaded as .xlsx.</summary>
    [HttpGet("ledger/export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null, [FromQuery] string? type = null,
        [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null, CancellationToken ct = default)
    {
        var file = await _mediator.Send(new ExportCashLedgerXlsxQuery(search, type, dateFrom, dateTo), ct);
        return File(file.Content, file.ContentType, file.OriginalFileName);
    }

    /// <summary>POST /api/cash-funds/ledger — Angular's CashLedgerRepository.addTransaction(). Owner deposit / withdrawal / expense / adjustment only — see AddCashTransactionCommandHandler. Admin only.</summary>
    [HttpPost("ledger")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CashLedgerEntryDto>> AddTransaction(AddCashTransactionRequest request, CancellationToken ct)
    {
        var command = new AddCashTransactionCommand(request.TransactionType, request.Amount, request.Remarks ?? string.Empty, request.TransactionDate, request.IsCashIn);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>PUT /api/cash-funds/ledger/{id} — the grid's row-menu "Edit" action. Manually-entered rows only. Admin only.</summary>
    [HttpPut("ledger/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CashLedgerEntryDto>> EditTransaction(string id, EditCashTransactionRequest request, CancellationToken ct)
    {
        var command = new EditCashTransactionCommand(id, request.TransactionType, request.Amount, request.Remarks ?? string.Empty, request.TransactionDate, request.IsCashIn);
        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/cash-funds/ledger/{id} — the grid's row-menu "Delete" action. Manually-entered rows only. Admin only.</summary>
    [HttpDelete("ledger/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTransaction(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCashTransactionCommand(id), ct);
        return NoContent();
    }
}

public sealed record AddCashTransactionRequest(string TransactionType, decimal Amount, string? Remarks, string? TransactionDate = null, bool? IsCashIn = null);

public sealed record EditCashTransactionRequest(string TransactionType, decimal Amount, string? Remarks, string TransactionDate, bool? IsCashIn = null);
