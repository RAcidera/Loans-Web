using LoanManagementSystem.Application.Common.DTOs;
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
}
