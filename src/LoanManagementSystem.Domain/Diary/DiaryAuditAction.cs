namespace LoanManagementSystem.Domain.Diary;

/// <summary>The Diary changes the audit trail tracks (requirements §24) — see LoanAuditAction for the loan-side counterpart this mirrors.</summary>
public enum DiaryAuditAction
{
    Created,
    Updated,
    Deleted,
    SnapshotCaptured,
    ReminderChanged,
    LinkedCustomerChanged,
    LinkedLoanChanged,
}
