namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Shaped to match the Angular frontend's AppUser entity field-for-field.
/// Deliberately excludes PasswordHash — this DTO must never let a hash
/// leave the process.
/// </summary>
public sealed record UserDto(
    string UserId,
    string Username,
    string Role,
    string Status,
    string CreatedAt
);
