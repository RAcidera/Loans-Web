using LoanManagementSystem.Application.Auth.Commands.Login;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_userRepository.Object, _passwordHasher.Object, _tokenGenerator.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = User.Create("admin", "hashed-value", UserRole.Admin);
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed-value")).Returns(true);
        _tokenGenerator.Setup(t => t.Generate(user)).Returns(new IssuedToken("jwt-token-value", DateTime.UtcNow.AddHours(1)));

        var result = await _handler.Handle(new LoginCommand("admin", "correct-password"), CancellationToken.None);

        Assert.Equal("jwt-token-value", result.Token);
        Assert.Equal("admin", result.Username);
        Assert.Equal("admin", result.Role); // lowercased for the wire, matches Angular's UserRole union
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsAuthenticationFailed()
    {
        var user = User.Create("admin", "hashed-value", UserRole.Admin);
        _userRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong-password", "hashed-value")).Returns(false);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _handler.Handle(new LoginCommand("admin", "wrong-password"), CancellationToken.None));

        _tokenGenerator.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUsername_ThrowsAuthenticationFailed_SameAsWrongPassword()
    {
        // Security property: this must NOT throw a different exception (or
        // return a different message) than the wrong-password case, or the
        // endpoint becomes a username enumeration oracle. See the comment
        // on LoginCommandHandler itself.
        _userRepository.Setup(r => r.GetByUsernameAsync("no-such-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _handler.Handle(new LoginCommand("no-such-user", "anything"), CancellationToken.None));

        // Same message text as the wrong-password case (see the test
        // above and LoginCommandHandler's single throw site) — asserted
        // here by construction, since both paths throw from the same line.
        Assert.Equal("Invalid username or password.", exception.Message);
    }

    [Fact]
    public async Task Handle_PasswordHasher_NeverCalledWithNullUser()
    {
        // Verify.Verify() would still be called correctly even for an
        // unknown user? No — Handle should short-circuit via `user is null`
        // in its guard clause and never call Verify at all for a missing
        // user, avoiding a null-reference risk in whatever hasher
        // implementation eventually runs here.
        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _handler.Handle(new LoginCommand("ghost", "whatever"), CancellationToken.None));

        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
