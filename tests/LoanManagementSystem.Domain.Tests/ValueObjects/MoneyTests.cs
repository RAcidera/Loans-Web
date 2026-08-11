using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_RoundsToTwoDecimalPlaces()
    {
        var money = Money.Of(10.005m);
        Assert.Equal(10.01m, money.Amount); // AwayFromZero rounding
    }

    [Fact]
    public void Of_NegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(-1));
    }

    [Fact]
    public void Add_SumsTwoAmounts()
    {
        var result = Money.Of(100).Add(Money.Of(50));
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public void Subtract_ClampsAtZero_RatherThanGoingNegative()
    {
        // Business decision documented on Money.Subtract: an overpayment
        // clamps to zero rather than throwing, since going negative is a
        // refund/credit decision, not an invariant violation.
        var result = Money.Of(50).Subtract(Money.Of(80));
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void Subtract_NormalCase_ReturnsDifference()
    {
        var result = Money.Of(100).Subtract(Money.Of(30));
        Assert.Equal(70m, result.Amount);
    }

    [Fact]
    public void TwoMoneyInstances_WithSameAmount_AreEqual()
    {
        // ValueObject equality — Money has no identity of its own.
        Assert.Equal(Money.Of(42), Money.Of(42));
    }

    [Fact]
    public void IsGreaterThan_ComparesAmounts()
    {
        Assert.True(Money.Of(100).IsGreaterThan(Money.Of(50)));
        Assert.False(Money.Of(50).IsGreaterThan(Money.Of(100)));
    }
}
