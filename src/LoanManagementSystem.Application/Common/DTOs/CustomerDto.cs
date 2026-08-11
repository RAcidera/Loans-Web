namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Shaped to match the Angular frontend's Customer entity field-for-field
/// (camelCase via System.Text.Json's default policy configured in Program.cs).
/// </summary>
public sealed record CustomerDto(
    string CustomerId,
    string CustomerCode,
    string FullName,
    string Address,
    string ContactNumber,
    string BorrowerType,
    string NicknameAlias,
    string Notes,
    string Status,
    string CreatedAt
);
