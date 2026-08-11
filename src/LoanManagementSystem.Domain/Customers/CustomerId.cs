namespace LoanManagementSystem.Domain.Customers;

/// <summary>
/// Strongly-typed id wrapping Guid — prevents accidentally passing a
/// LoanId where a CustomerId is expected, which a bare Guid parameter
/// would allow. Serializes to/from a plain string for the API and the
/// Angular frontend, which treat ids as opaque strings.
/// </summary>
public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());

    public static CustomerId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
