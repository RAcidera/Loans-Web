namespace LoanManagementSystem.Domain.Promises;

public readonly record struct PromiseAuditLogEntryId(Guid Value)
{
    public static PromiseAuditLogEntryId New() => new(Guid.NewGuid());

    public static PromiseAuditLogEntryId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
