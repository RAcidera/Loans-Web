namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Requirements §20 — shaped to match the Angular frontend's PromiseToPay entity field-for-field.</summary>
public sealed record PromiseToPayDto(
    string PromiseId,
    string CustomerId,
    string CustomerName,
    string LoanId,
    string LoanNumber,
    string PromiseDate,
    decimal Amount,
    string Notes,
    string Status,
    string CreatedBy,
    string CreatedAt,
    string ModifiedBy,
    string ModifiedAt
);

/// <summary>Backs the Promise Detail's audit section (requirements §24).</summary>
public sealed record PromiseAuditLogEntryDto(string AuditLogId, string PromiseId, string Action, string Description, string PerformedBy, string OccurredAt);
