namespace LoanManagementSystem.Domain.Loans;

public readonly record struct LoanAuditLogEntryId(Guid Value)
{
    public static LoanAuditLogEntryId New() => new(Guid.NewGuid());
    public static LoanAuditLogEntryId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
