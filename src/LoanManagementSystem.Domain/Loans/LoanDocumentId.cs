namespace LoanManagementSystem.Domain.Loans;

public readonly record struct LoanDocumentId(Guid Value)
{
    public static LoanDocumentId New() => new(Guid.NewGuid());
    public static LoanDocumentId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
