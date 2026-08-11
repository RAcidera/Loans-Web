using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResultDto>;

public sealed record LoginResultDto(string Token, DateTime ExpiresAtUtc, string Username, string Role);

/// <summary>
/// Deliberately doesn't distinguish "user not found" from "wrong password"
/// in the exception message — both should look identical to a caller, so
/// a bad-actor can't use this endpoint to enumerate valid usernames.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);

        // Deactivated accounts fail exactly like a bad password — no
        // information leak about *why* the login was rejected.
        if (user is null || user.Status != UserStatus.Active || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationFailedException("Invalid username or password.");

        var issued = _tokenGenerator.Generate(user);
        return new LoginResultDto(issued.Token, issued.ExpiresAtUtc, user.Username, user.Role.ToString().ToLowerInvariant());
    }
}
