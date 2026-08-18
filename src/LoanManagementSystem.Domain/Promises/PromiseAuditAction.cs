namespace LoanManagementSystem.Domain.Promises;

/// <summary>The promise-to-pay changes the audit trail tracks (requirements §24).</summary>
public enum PromiseAuditAction
{
    Created,
    Updated,
    Kept,
    Missed,
    Rescheduled,
    Cancelled,
}
