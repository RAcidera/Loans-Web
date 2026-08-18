namespace LoanManagementSystem.Domain.Diary;

public readonly record struct DiaryCategoryId(Guid Value)
{
    public static DiaryCategoryId New() => new(Guid.NewGuid());

    public static DiaryCategoryId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
