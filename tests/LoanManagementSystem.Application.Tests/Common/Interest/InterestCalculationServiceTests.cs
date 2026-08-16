using LoanManagementSystem.Application.Common.Interest;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Common.Interest;

/// <summary>Verifies InterestCalculationService against the Interest Earned Report spec's own worked examples (§3, §5, §7, §8, §23).</summary>
public class InterestCalculationServiceTests
{
    private readonly InterestCalculationService _service = new();

    private static Loan CreateLoan(decimal principal, decimal rate, DateOnly startDate, int termDays = 60, int paymentTermsMonths = 2, decimal? interestAmount = null) =>
        Loan.Originate(
            CustomerId.New(), Money.Of(principal), InterestRate.Of(rate), startDate, termDays, paymentTermsMonths,
            interestAmount.HasValue ? Money.Of(interestAmount.Value) : null);

    [Fact]
    public void EarnedThisPeriod_MatchesSpecWorkedExample_22DaysAt5PerDay()
    {
        // Spec §4: Principal 10,000, rate 3% -> contract interest 300, 60-day term,
        // daily interest 5/day. Loan starts Aug 10; report Aug 1-Aug 31 => 22 eligible
        // days (Aug 10 through Aug 31 inclusive) => 110.00 earned this period.
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 8, 10), termDays: 60);

        var breakdown = _service.Calculate(loan, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal(300m, breakdown.OriginalContractInterest);
        Assert.Equal(0m, breakdown.OriginalEarnedBeforePeriod);
        Assert.Equal(110m, breakdown.OriginalEarnedThisPeriod);
    }

    [Fact]
    public void EarnedInterest_NeverExceedsContractInterest_EvenLongAfterMaturityWithoutExtension()
    {
        // Spec §8: 60-day loan, contract interest 300. At day 90 without an
        // extension, earned interest must still be exactly 300, not more.
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 1, 1), termDays: 60);

        var breakdown = _service.Calculate(loan, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays(90));

        Assert.Equal(300m, breakdown.OriginalTotalEarned);
    }

    [Fact]
    public void FinalDayReconciliation_NoRoundingDrift_EvenWithNonTerminatingDivision()
    {
        // Spec §23: Contract interest 100, term 30 days -> 100/30 = 3.333...
        // At maturity the total earned must be EXACTLY 100.00, not 99.90/100.01.
        // paymentTermsMonths=1 -> the service derives termDays as months*30 = 30,
        // matching how CreateLoanCommand always keeps these two in sync in practice.
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 1, 1), termDays: 30, paymentTermsMonths: 1, interestAmount: 100m);

        var atMaturity = _service.Calculate(loan, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays(29));

        Assert.Equal(100.00m, atMaturity.OriginalTotalEarned);
    }

    [Fact]
    public void EarnedBeforePeriod_IsZero_WhenLoanStartsAfterFromDate()
    {
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 8, 10), termDays: 60);

        var breakdown = _service.Calculate(loan, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal(0m, breakdown.OriginalEarnedBeforePeriod);
    }

    [Fact]
    public void Extension_IsCalculatedIndependently_StartingWhereTheOriginalTermEnds()
    {
        // Spec §5: Original Aug 10 - due date after 60 days; Extension #1 adds
        // 30 days with its own AdditionalChargesAmount treated as the amount
        // earned via the same daily-accrual formula over its own period.
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 8, 10), termDays: 60);
        var originalDueDate = loan.DueDate; // StartDate + 60 (this codebase's exclusive-end convention)
        loan.Extend(30, Money.Of(150m), "extension", originalDueDate);

        // Fully within the extension period: 30 days extension, report window covers all 30.
        var breakdown = _service.Calculate(loan, originalDueDate, originalDueDate.AddDays(29));

        Assert.Equal(150m, breakdown.ExtensionContractInterest);
        Assert.Equal(150m, breakdown.ExtensionTotalEarned);
        // Original period is already fully earned (its own term has fully elapsed by the extension's start).
        Assert.Equal(300m, breakdown.OriginalTotalEarned);
    }

    [Fact]
    public void MultipleExtensions_AreEachCappedAtTheirOwnAmount()
    {
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 1, 1), termDays: 60);
        var dueDate1 = loan.DueDate;
        loan.Extend(30, Money.Of(150m), "ext1", dueDate1);
        var dueDate2 = loan.DueDate;
        loan.Extend(30, Money.Of(90m), "ext2", dueDate2);

        // Report window covering well past both extensions.
        var breakdown = _service.Calculate(loan, dueDate2, dueDate2.AddDays(60));

        Assert.Equal(240m, breakdown.ExtensionContractInterest); // 150 + 90
        Assert.Equal(240m, breakdown.ExtensionTotalEarned);
    }

    [Fact]
    public void InterestAdjustment_ReflectsManualOverride_AgainstRateBasedOriginal()
    {
        // Spec §2.4/§7: Original (rate-based) 300, manually adjusted to 180 (early
        // settlement discount) -> Adjustment = -120, and earned interest must cap
        // at the ADJUSTED amount, never recognizing the original 300 again.
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 1, 1), termDays: 60, interestAmount: 180m);

        var breakdown = _service.Calculate(loan, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays(90));

        Assert.Equal(300m, breakdown.OriginalContractInterest);
        Assert.Equal(180m, breakdown.AdjustedContractInterest);
        Assert.Equal(-120m, breakdown.InterestAdjustment);
        Assert.Equal(180m, breakdown.OriginalTotalEarned); // capped at the adjusted amount, not 300
    }

    [Fact]
    public void InterestAdjustment_IsZero_WhenInterestWasNeverManuallyOverridden()
    {
        var loan = CreateLoan(10_000m, 0.03m, new DateOnly(2026, 1, 1), termDays: 60);

        var breakdown = _service.Calculate(loan, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1).AddDays(60));

        Assert.Equal(0m, breakdown.InterestAdjustment);
    }
}
