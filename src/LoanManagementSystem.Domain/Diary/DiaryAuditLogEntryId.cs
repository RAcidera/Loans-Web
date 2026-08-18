namespace LoanManagementSystem.Domain.Diary;

public readonly record struct DiaryAuditLogEntryId(Guid Value)
{
    public static DiaryAuditLogEntryId New() => new(Guid.NewGuid());

    public static DiaryAuditLogEntryId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
