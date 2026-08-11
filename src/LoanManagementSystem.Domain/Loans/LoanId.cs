namespace LoanManagementSystem.Domain.Loans;

public readonly record struct LoanId(Guid Value)
{
    public static LoanId New() => new(Guid.NewGuid());

    public static LoanId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
