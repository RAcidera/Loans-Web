using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.ValueObjects;

/// <summary>
/// A flat interest rate, stored as a fraction (0.03 = 3%), per SRS 3.2's
/// default 3% fixed rate. Flat and non-compounding: the interest for a
/// loan is principal * rate, computed once at origination, not recomputed
/// per period.
/// </summary>
public sealed class InterestRate : ValueObject
{
    public decimal Value { get; }

    private InterestRate(decimal value)
    {
        Value = value;
    }

    public static InterestRate Of(decimal value)
    {
        if (value < 0 || value > 1)
            throw new DomainException($"Interest rate must be between 0 and 1 as a fraction (got {value}).");
        return new InterestRate(value);
    }

    public static InterestRate Default => new(0.03m);

    public Money CalculateInterest(Money principal) => Money.Of(Math.Round(principal.Amount * Value, 2));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:P0}";
}
