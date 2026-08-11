namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Customers list-page row shape — CustomerDto's fields plus a per-row loan
/// count, computed server-side (see GetCustomersPageQueryHandler) so the
/// paged /customers/page endpoint doesn't require the frontend to
/// separately load every loan just to count them.
/// </summary>
public sealed record CustomerListItemDto(
    string CustomerId,
    string CustomerCode,
    string FullName,
    string Address,
    string ContactNumber,
    string BorrowerType,
    string NicknameAlias,
    string Notes,
    string Status,
    string CreatedAt,
    int LoanCount
);
