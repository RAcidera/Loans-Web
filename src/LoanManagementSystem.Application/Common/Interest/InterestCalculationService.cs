using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Application.Common.Interest;

/// <summary>
/// Elapsed-time ("earned") interest, independent of payment timing — the
/// Interest Earned Report's core engine, reusable later by the Dashboard,
/// Loan Detail, and Statement of Account for the same figures.
///
/// Modeling decisions made against this domain's actual shape (there is no
/// raw TermDays, no per-extension interest field, and no adjustment audit
/// trail — see the notes below for why each derived figure is computed the
/// way it is):
///
/// - Contract Interest = InterestRate.CalculateInterest(PrincipalAmount) —
///   recomputed fresh from the loan's CURRENT rate, not the (nonexistent)
///   original-at-origination value. This is what "OriginalContractInterest"
///   means throughout this service: not "the first number ever agreed,"
///   but "what the rate formula alone would produce," which is the only
///   version of that number this domain can still answer.
/// - Adjusted Contract Interest = Loan.TotalInterest — the current stored
///   value, which a manual EditLoan(interestAmount: ...) override may have
///   pushed away from the rate-based figure. InterestAdjustment is simply
///   the delta between the two; this recovers the report's "Interest
///   Adjustment" concept without needing a new audit-trail field, at the
///   cost of only ever knowing the NET adjustment, not its history/reason.
/// - Extension "interest" = the extension's AdditionalChargesAmount. The
///   domain deliberately has no separate extension-interest field (it was
///   removed; "Additional Charges" alone represents what an extension
///   earns) — so this service treats that charge as the amount earned via
///   the same daily-accrual formula as the original loan, over the
///   extension's own period. This is the only way to give the report's
///   "Extension Interest" concept a real, non-zero number using data that
///   actually exists.
/// </summary>
public sealed class InterestCalculationService : IInterestCalculationService
{
    public LoanInterestBreakdown Calculate(Loan loan, DateOnly fromDate, DateOnly toDate)
    {
        var originalContractInterest = loan.InterestRate.CalculateInterest(loan.PrincipalAmount).Amount;
        var adjustedContractInterest = loan.TotalInterest.Amount;
        var adjustment = adjustedContractInterest - originalContractInterest;

        var termDays = Math.Max(1, loan.PaymentTermsMonths * 30);
        var originalPeriod = CalculatePeriod("Original Loan", loan.StartDate, termDays, adjustedContractInterest, fromDate, toDate);

        var extensionPeriods = new List<InterestPeriod>();
        var runningStart = loan.StartDate.AddDays(termDays);
        var extensionContractInterest = 0m;

        // Ordered by ExtensionDate, the only chronological signal LoanExtension
        // carries — see the doc comment on the domain type for the caveat that
        // this can theoretically diverge from Loan.DueDate if extensions were
        // edited out of creation order. Acceptable for a report (best-effort,
        // not a ledger of record).
        foreach (var extension in loan.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc))
        {
            var label = $"Extension #{extensionPeriods.Count + 1}";
            var period = CalculatePeriod(label, runningStart, extension.ExtensionDays, extension.AdditionalChargesAmount.Amount, fromDate, toDate);
            extensionPeriods.Add(period);
            extensionContractInterest += extension.AdditionalChargesAmount.Amount;
            runningStart = runningStart.AddDays(extension.ExtensionDays);
        }

        return new LoanInterestBreakdown(
            OriginalContractInterest: Math.Round(originalContractInterest, 2),
            AdjustedContractInterest: Math.Round(adjustedContractInterest, 2),
            InterestAdjustment: Math.Round(adjustment, 2),
            ExtensionContractInterest: Math.Round(extensionContractInterest, 2),
            OriginalEarnedBeforePeriod: originalPeriod.EarnedBeforePeriod,
            OriginalEarnedThisPeriod: originalPeriod.EarnedThisPeriod,
            OriginalTotalEarned: originalPeriod.TotalEarnedThroughToDate,
            ExtensionEarnedBeforePeriod: Math.Round(extensionPeriods.Sum(p => p.EarnedBeforePeriod), 2),
            ExtensionEarnedThisPeriod: Math.Round(extensionPeriods.Sum(p => p.EarnedThisPeriod), 2),
            ExtensionTotalEarned: Math.Round(extensionPeriods.Sum(p => p.TotalEarnedThroughToDate), 2),
            Periods: new List<InterestPeriod> { originalPeriod }.Concat(extensionPeriods).ToList()
        );
    }

    private static InterestPeriod CalculatePeriod(string label, DateOnly periodStart, int termDays, decimal contractAmount, DateOnly fromDate, DateOnly toDate)
    {
        // Unrounded until the very end (spec §22/§23) — rounding the daily
        // rate first, then multiplying by elapsed days, accumulates drift
        // and can miss the "earns exactly the contract amount at maturity"
        // guarantee (e.g. ₱100/30 days rounded to ₱3.33/day loses ₱0.10 by
        // day 30). Math.Min against the untouched contractAmount below is
        // what makes that guarantee exact, decimal division never rounded.
        var dailyInterest = contractAmount / termDays;

        var earnedBefore = EarnedAsOf(contractAmount, dailyInterest, periodStart, fromDate);
        var totalThroughToDate = EarnedAsOf(contractAmount, dailyInterest, periodStart, toDate.AddDays(1));
        var earnedThisPeriod = totalThroughToDate - earnedBefore;

        var periodEndInclusive = periodStart.AddDays(termDays - 1);
        var earnedDaysThisPeriod = EarnedDaysInPeriod(periodStart, termDays, fromDate, toDate);

        return new InterestPeriod(
            Label: label,
            PeriodStart: periodStart,
            PeriodEndInclusive: periodEndInclusive,
            TermDays: termDays,
            ContractAmount: Math.Round(contractAmount, 2),
            DailyInterest: Math.Round(dailyInterest, 4),
            EarnedDaysThisPeriod: earnedDaysThisPeriod,
            EarnedBeforePeriod: Math.Round(earnedBefore, 2),
            EarnedThisPeriod: Math.Round(earnedThisPeriod, 2),
            TotalEarnedThroughToDate: Math.Round(totalThroughToDate, 2)
        );
    }

    /// <summary>
    /// Daily-accrual earned interest for the days strictly before
    /// <paramref name="asOfExclusive"/>, capped at contractAmount — spec §3.2.
    /// Using DateOnly.DayNumber (days since 0001-01-01) for the elapsed-day
    /// count keeps this exact and avoids re-deriving day differences with
    /// different logic elsewhere (spec §24's "centralize term-day rules").
    /// </summary>
    private static decimal EarnedAsOf(decimal contractAmount, decimal dailyInterest, DateOnly periodStart, DateOnly asOfExclusive)
    {
        var elapsedDays = asOfExclusive.DayNumber - periodStart.DayNumber;
        if (elapsedDays <= 0) return 0m;
        return Math.Min(contractAmount, dailyInterest * elapsedDays);
    }

    /// <summary>How many of this period's days fall within [fromDate, toDate] — the drill-down's "Earned Days" figure.</summary>
    private static int EarnedDaysInPeriod(DateOnly periodStart, int termDays, DateOnly fromDate, DateOnly toDate)
    {
        var beforeDays = Math.Clamp(fromDate.DayNumber - periodStart.DayNumber, 0, termDays);
        var throughDays = Math.Clamp(toDate.AddDays(1).DayNumber - periodStart.DayNumber, 0, termDays);
        return Math.Max(0, throughDays - beforeDays);
    }
}
