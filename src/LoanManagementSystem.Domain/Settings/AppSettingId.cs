namespace LoanManagementSystem.Domain.Settings;

public readonly record struct AppSettingId(Guid Value)
{
    public static AppSettingId New() => new(Guid.NewGuid());
    public static AppSettingId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
