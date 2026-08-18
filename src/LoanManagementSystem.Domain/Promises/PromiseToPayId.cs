namespace LoanManagementSystem.Domain.Promises;

public readonly record struct PromiseToPayId(Guid Value)
{
    public static PromiseToPayId New() => new(Guid.NewGuid());

    public static PromiseToPayId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
