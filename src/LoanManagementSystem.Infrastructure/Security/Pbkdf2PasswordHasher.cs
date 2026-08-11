using System.Security.Cryptography;
using LoanManagementSystem.Application.Common.Security;

namespace LoanManagementSystem.Infrastructure.Security;

/// <summary>
/// PBKDF2 via Rfc2898DeriveBytes — built into .NET, no extra NuGet package
/// needed (unlike BCrypt.Net). 100,000 iterations follows OWASP's current
/// minimum recommendation for PBKDF2-SHA256. Output format is
/// "{iterations}.{salt-base64}.{hash-base64}" so the iteration count and
/// salt travel with the hash — this makes it possible to raise the
/// iteration count later without invalidating already-stored hashes; only
/// hashes created with the old count would still verify with more work.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        // Constant-time comparison — a naive == or SequenceEqual on the
        // hash bytes would let an attacker infer how many leading bytes
        // matched from response timing.
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
