using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Settings.Commands.UpdateBusinessTimeZone;
using LoanManagementSystem.Application.Settings.Queries.GetAppSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize] // any authenticated user (Admin or Staff) — every page needs the business timezone id and current business date to render
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/settings — the General Settings area's current values.</summary>
    [HttpGet]
    public async Task<ActionResult<AppSettingsDto>> Get(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAppSettingsQuery(), ct));

    /// <summary>PUT /api/settings/business-time-zone — Admin only.</summary>
    [HttpPut("business-time-zone")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AppSettingsDto>> UpdateBusinessTimeZone(UpdateBusinessTimeZoneRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UpdateBusinessTimeZoneCommand(request.TimeZoneId), ct));
}

public sealed record UpdateBusinessTimeZoneRequest(string TimeZoneId);
