namespace LoanManagementSystem.Application.Common.Security;

/// <summary>
/// Abstraction over the hashing algorithm so Application code (and tests)
/// never need to know whether it's PBKDF2, bcrypt, or anything else.
/// Implemented in Infrastructure — see Pbkdf2PasswordHasher.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
