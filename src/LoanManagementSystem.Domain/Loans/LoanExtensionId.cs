namespace LoanManagementSystem.Domain.Loans;

public readonly record struct LoanExtensionId(Guid Value)
{
    public static LoanExtensionId New() => new(Guid.NewGuid());
    public static LoanExtensionId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
