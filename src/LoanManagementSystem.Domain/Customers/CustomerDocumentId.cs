namespace LoanManagementSystem.Domain.Customers;

public readonly record struct CustomerDocumentId(Guid Value)
{
    public static CustomerDocumentId New() => new(Guid.NewGuid());
    public static CustomerDocumentId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
