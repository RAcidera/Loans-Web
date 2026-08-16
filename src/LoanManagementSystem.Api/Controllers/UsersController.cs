using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Users.Commands.ActivateUser;
using LoanManagementSystem.Application.Users.Commands.AdminResetPassword;
using LoanManagementSystem.Application.Users.Commands.ChangePassword;
using LoanManagementSystem.Application.Users.Commands.ChangeUserRole;
using LoanManagementSystem.Application.Users.Commands.CreateUser;
using LoanManagementSystem.Application.Users.Commands.DeactivateUser;
using LoanManagementSystem.Application.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // any authenticated user (Admin or Staff) — see per-action overrides below
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/users — Admin only. SRS "Staff cannot see the user list".</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetUsersQuery(), ct));

    /// <summary>POST /api/users — Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Username, request.Password, request.Role);
        var created = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), created);
    }

    /// <summary>
    /// PUT /api/users/me/password — any authenticated user, changes only the
    /// CALLER's own password. Resolved from the caller's own token (never a
    /// route id), so there's no separate self-or-admin authorization check
    /// to get wrong.
    /// </summary>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var username = User.Identity!.Name!;
        var command = new ChangePasswordCommand(username, request.CurrentPassword, request.NewPassword);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>POST /api/users/{id}/deactivate — Admin only.</summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        var command = new DeactivateUserCommand(id, User.Identity!.Name!);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>POST /api/users/{id}/activate — Admin only. Reverses a Deactivate.</summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        await _mediator.Send(new ActivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>PUT /api/users/{id}/role — Admin only. Switches a user between Staff and Admin.</summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> ChangeRole(string id, ChangeUserRoleRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ChangeUserRoleCommand(id, request.Role), ct));

    /// <summary>
    /// PUT /api/users/{id}/password — Admin only. Resets ANOTHER user's
    /// forgotten password (no current-password check, unlike PUT
    /// /me/password above) — the Settings page's "Reset password" action.
    /// </summary>
    [HttpPut("{id}/password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminResetPassword(string id, AdminResetPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new AdminResetPasswordCommand(id, request.NewPassword), ct);
        return NoContent();
    }
}

public sealed record CreateUserRequest(string Username, string Password, string Role);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ChangeUserRoleRequest(string Role);
public sealed record AdminResetPasswordRequest(string NewPassword);
