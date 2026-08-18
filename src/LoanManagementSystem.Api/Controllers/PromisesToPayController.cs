using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Promises.Commands.CancelPromise;
using LoanManagementSystem.Application.Promises.Commands.CreatePromise;
using LoanManagementSystem.Application.Promises.Commands.DeletePromise;
using LoanManagementSystem.Application.Promises.Commands.MarkPromiseKept;
using LoanManagementSystem.Application.Promises.Commands.MarkPromiseMissed;
using LoanManagementSystem.Application.Promises.Commands.ReschedulePromise;
using LoanManagementSystem.Application.Promises.Commands.UpdatePromise;
using LoanManagementSystem.Application.Promises.Queries.GetPromiseAuditLog;
using LoanManagementSystem.Application.Promises.Queries.GetPromiseById;
using LoanManagementSystem.Application.Promises.Queries.GetPromisesByCustomer;
using LoanManagementSystem.Application.Promises.Queries.GetPromisesByLoan;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

/// <summary>
/// Requirements §22. No standalone Promises list page exists in the
/// Angular app (per the implementation plan's Phase 3 assumption — payments
/// being irregular is framed as a customer/loan-level concern, surfaced as
/// a "Promises" tab on the Customer Profile / Loan Details pages instead),
/// so GET's collection form is filtered by customerId or loanId rather than
/// returning every promise unfiltered.
/// </summary>
[ApiController]
[Route("api/promises-to-pay")]
[Authorize]
public class PromisesToPayController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromisesToPayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/promises-to-pay?customerId=&amp;loanId= — exactly one of the two must be given.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PromiseToPayDto>>> GetPromises(
        [FromQuery] string? customerId, [FromQuery] string? loanId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(customerId))
            return Ok(await _mediator.Send(new GetPromisesByCustomerQuery(customerId), ct));
        if (!string.IsNullOrWhiteSpace(loanId))
            return Ok(await _mediator.Send(new GetPromisesByLoanQuery(loanId), ct));

        return BadRequest("Either customerId or loanId is required.");
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PromiseToPayDto>> GetById(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetPromiseByIdQuery(id), ct));

    [HttpGet("{id}/audit-log")]
    public async Task<ActionResult<List<PromiseAuditLogEntryDto>>> GetAuditLog(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetPromiseAuditLogQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<PromiseToPayDto>> Create(CreatePromiseRequest request, CancellationToken ct)
    {
        var command = new CreatePromiseCommand(request.CustomerId, request.LoanId, request.PromiseDate, request.Amount, request.Notes ?? "", User.Identity?.Name ?? "unknown");
        var created = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.PromiseId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PromiseToPayDto>> Update(string id, UpdatePromiseRequest request, CancellationToken ct)
    {
        var command = new UpdatePromiseCommand(id, request.PromiseDate, request.Amount, request.Notes ?? "", User.Identity?.Name ?? "unknown");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeletePromiseCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id}/kept")]
    public async Task<ActionResult<PromiseToPayDto>> MarkKept(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new MarkPromiseKeptCommand(id, User.Identity?.Name ?? "unknown"), ct));

    [HttpPost("{id}/missed")]
    public async Task<ActionResult<PromiseToPayDto>> MarkMissed(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new MarkPromiseMissedCommand(id, User.Identity?.Name ?? "unknown"), ct));

    [HttpPost("{id}/reschedule")]
    public async Task<ActionResult<PromiseToPayDto>> Reschedule(string id, ReschedulePromiseRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ReschedulePromiseCommand(id, request.NewPromiseDate, User.Identity?.Name ?? "unknown"), ct));

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PromiseToPayDto>> Cancel(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new CancelPromiseCommand(id, User.Identity?.Name ?? "unknown"), ct));
}

public sealed record CreatePromiseRequest(string CustomerId, string LoanId, string PromiseDate, decimal Amount, string? Notes = null);
public sealed record UpdatePromiseRequest(string PromiseDate, decimal Amount, string? Notes = null);
public sealed record ReschedulePromiseRequest(string NewPromiseDate);
