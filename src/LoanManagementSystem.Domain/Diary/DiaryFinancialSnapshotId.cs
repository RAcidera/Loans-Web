namespace LoanManagementSystem.Domain.Diary;

public readonly record struct DiaryFinancialSnapshotId(Guid Value)
{
    public static DiaryFinancialSnapshotId New() => new(Guid.NewGuid());

    public static DiaryFinancialSnapshotId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
