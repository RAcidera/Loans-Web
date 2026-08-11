using LoanManagementSystem.Domain.Identity;

namespace LoanManagementSystem.Application.Common.Security;

public sealed record IssuedToken(string Token, DateTime ExpiresAtUtc);

/// <summary>
/// Abstraction over JWT creation, implemented in Infrastructure
/// (JwtTokenGenerator) using the signing key/issuer/audience configured in
/// appsettings. Kept out of the Domain layer entirely — a User aggregate
/// has no business knowing what a JWT is.
/// </summary>
public interface IJwtTokenGenerator
{
    IssuedToken Generate(User user);
}
