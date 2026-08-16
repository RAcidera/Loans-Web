using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Identity;

/// <summary>
/// User aggregate root, matching the SRS's Users table (user_id, username,
/// password_hash, role, created_at). Only ever stores a password HASH,
/// never a plaintext password — the hashing algorithm itself is an
/// infrastructure concern (see IPasswordHasher in the Application layer),
/// so this aggregate just holds whatever hash string it's given and never
/// tries to verify or produce one itself.
/// </summary>
public class User : AggregateRoot<UserId>
{
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF Core

    private User(UserId id, string username, string passwordHash, UserRole role)
        : base(id)
    {
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static User Create(string username, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("A user must have a username.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A user must have a password hash.");

        return new User(UserId.New(), username.Trim(), passwordHash, role);
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("A password hash cannot be empty.");
        PasswordHash = newPasswordHash;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
    }

    public void Activate()
    {
        Status = UserStatus.Active;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
}
