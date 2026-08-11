using LoanManagementSystem.Application.CashLedger.Commands.AddCashTransaction;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashLedger;
using LoanManagementSystem.Application.CashLedger.Queries.GetCashSummary;
using LoanManagementSystem.Application.Common.DTOs;
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

    /// <summary>GET /api/cash-funds/summary — Angular's CashLedgerRepository.getSummary(). Implements the SRS's Formulas 1-5.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<CashSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCashSummaryQuery(), ct));

    /// <summary>GET /api/cash-funds/ledger — Angular's CashLedgerRepository.getLedgerEntries().</summary>
    [HttpGet("ledger")]
    public async Task<ActionResult<List<CashLedgerEntryDto>>> GetLedger(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCashLedgerQuery(), ct));

    /// <summary>POST /api/cash-funds/ledger — Angular's CashLedgerRepository.addTransaction(). Owner deposit / withdrawal / expense only — see AddCashTransactionCommandHandler. Admin only.</summary>
    [HttpPost("ledger")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CashLedgerEntryDto>> AddTransaction(AddCashTransactionRequest request, CancellationToken ct)
    {
        var command = new AddCashTransactionCommand(request.TransactionType, request.Amount, request.Remarks ?? string.Empty);
        return Ok(await _mediator.Send(command, ct));
    }
}

public sealed record AddCashTransactionRequest(string TransactionType, decimal Amount, string? Remarks);
