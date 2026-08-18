using LoanManagementSystem.Application.Calendar.Queries.GetCalendarEvents;
using LoanManagementSystem.Application.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/calendar/events?from=&amp;to=&amp;types= — requirements §18/§19/§22. `types` is comma-separated (diary,reminder,loan_due,extension_due,promise); omitted returns every type.</summary>
    [HttpGet("events")]
    public async Task<ActionResult<List<CalendarEventDto>>> GetEvents(
        [FromQuery] string from, [FromQuery] string to, [FromQuery] string? types = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetCalendarEventsQuery(from, to, types), ct));
}
