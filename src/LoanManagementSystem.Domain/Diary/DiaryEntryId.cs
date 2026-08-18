namespace LoanManagementSystem.Domain.Diary;

public readonly record struct DiaryEntryId(Guid Value)
{
    public static DiaryEntryId New() => new(Guid.NewGuid());

    public static DiaryEntryId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
