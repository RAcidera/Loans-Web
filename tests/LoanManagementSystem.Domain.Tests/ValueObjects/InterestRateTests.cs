using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.ValueObjects;

public class InterestRateTests
{
    [Fact]
    public void Default_Is3Percent()
    {
        // SRS 3.2: "Fixed interest rate (default 3%)"
        Assert.Equal(0.03m, InterestRate.Default.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Of_OutOfRange_Throws(double value)
    {
        Assert.Throws<DomainException>(() => InterestRate.Of((decimal)value));
    }

    [Fact]
    public void CalculateInterest_IsFlatNotCompounding()
    {
        // 5000 principal at 3% => 150, computed once, not per period.
        var interest = InterestRate.Default.CalculateInterest(Money.Of(5000));
        Assert.Equal(150m, interest.Amount);
    }
}
