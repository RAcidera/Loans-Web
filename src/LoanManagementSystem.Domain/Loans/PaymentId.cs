namespace LoanManagementSystem.Domain.Loans;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.NewGuid());
    public static PaymentId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
