using LoanManagementSystem.Application.Auth.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/auth/login — the only endpoint in this API that doesn't
    /// require a token (obviously; you need this one to get a token in
    /// the first place). Demo credentials seeded by DbSeeder:
    /// admin / Admin@12345 (role: admin), staff / Staff@12345 (role: staff).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Username, request.Password), ct);
        return Ok(result);
    }
}

public sealed record LoginRequest(string Username, string Password);
