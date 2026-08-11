using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.ValueObjects;

/// <summary>
/// A non-negative monetary amount. Wrapping decimal in a value object means
/// "can this be negative?" and "what currency?" are answered once, here,
/// instead of scattered across every place principal/balance/fee arithmetic
/// happens. Single-currency (PHP) for this system, so no currency code is
/// tracked — add one if this ever needs to support more than one.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Of(decimal amount)
    {
        if (amount < 0)
            throw new DomainException($"A monetary amount cannot be negative (got {amount}).");
        return new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero));
    }

    public static Money Zero => new(0);

    public Money Add(Money other) => Of(Amount + other.Amount);

    public Money Subtract(Money other)
    {
        var result = Amount - other.Amount;
        // Clamp at zero rather than throw — a balance going slightly negative
        // due to an overpayment is a business decision (refund? credit?),
        // not an invariant violation worth blowing up the whole operation.
        return Of(Math.Max(result, 0));
    }

    public bool IsGreaterThan(Money other) => Amount > other.Amount;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("F2");
}
