using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Application.Common.Interest;

/// <summary>
/// One earning period's daily-accrual figures — either a loan's original
/// term or one of its extensions. See InterestCalculationService for the
/// formula (Interest Earned Report spec §3).
/// </summary>
public sealed record InterestPeriod(
    string Label,
    DateOnly PeriodStart,
    DateOnly PeriodEndInclusive,
    int TermDays,
    decimal ContractAmount,
    decimal DailyInterest,
    int EarnedDaysThisPeriod,
    decimal EarnedBeforePeriod,
    decimal EarnedThisPeriod,
    decimal TotalEarnedThroughToDate
);

/// <summary>
/// A loan's full interest breakdown for a reporting window: the original
/// period plus every extension, each calculated independently and then
/// aggregated. See InterestCalculationService's class doc comment for the
/// modeling decisions behind ContractInterest/Adjustment/ExtensionInterest.
/// </summary>
public sealed record LoanInterestBreakdown(
    decimal OriginalContractInterest,
    decimal AdjustedContractInterest,
    decimal InterestAdjustment,
    decimal ExtensionContractInterest,
    decimal OriginalEarnedBeforePeriod,
    decimal OriginalEarnedThisPeriod,
    decimal OriginalTotalEarned,
    decimal ExtensionEarnedBeforePeriod,
    decimal ExtensionEarnedThisPeriod,
    decimal ExtensionTotalEarned,
    List<InterestPeriod> Periods
);

public interface IInterestCalculationService
{
    /// <summary>Computes elapsed-time interest earned for a loan (original term + every extension) as of a reporting window [fromDate, toDate], both inclusive.</summary>
    LoanInterestBreakdown Calculate(Loan loan, DateOnly fromDate, DateOnly toDate);
}
