namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record LoanAuditLogEntryDto(
    string AuditLogId,
    string LoanId,
    string Action,
    string Description,
    string PerformedBy,
    string OccurredAt
);
