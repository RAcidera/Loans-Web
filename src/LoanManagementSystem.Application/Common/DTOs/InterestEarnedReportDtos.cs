using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>One row of the Interest Earned Report's detailed grid — one loan's daily-accrual breakdown for the selected reporting window.</summary>
public sealed record InterestEarnedRowDto(
    string LoanId,
    string LoanNumber,
    string CustomerId,
    string CustomerName,
    string LoanDate,
    string DueDate,
    decimal Principal,
    decimal ContractInterest,
    decimal ExtensionInterest,
    decimal EarnedBeforePeriod,
    decimal EarnedThisPeriod,
    decimal TotalEarned,
    decimal Adjustment,
    decimal FinalEarned,
    string Status,
    string Classification,
    /// <summary>The original-loan and extension components of EarnedThisPeriod, before the InterestType filter combines/zeroes them — used only to build the summary KPIs without recalculating each loan a second time.</summary>
    decimal OriginalEarnedThisPeriod,
    decimal ExtensionEarnedThisPeriod
);

/// <summary>Footer totals for the grid — sums across every record matching the report's filters, not just the current page.</summary>
public sealed record InterestEarnedTotalsDto(
    decimal Principal,
    decimal ContractInterest,
    decimal ExtensionInterest,
    decimal EarnedBeforePeriod,
    decimal EarnedThisPeriod,
    decimal TotalEarned,
    decimal Adjustment,
    decimal FinalEarned,
    int Count
);

/// <summary>
/// The report's six summary cards. InterestCollected/InterestReceivable are
/// null rather than estimated — this codebase's Payment doesn't yet
/// allocate a payment between principal and interest, so "how much of what
/// was collected was interest" isn't a real, non-fabricated number today
/// (Interest Earned Report spec §2.5 explicitly calls for omitting/marking
/// unavailable rather than estimating from total payments).
/// </summary>
public sealed record InterestEarnedSummaryDto(
    decimal TotalEarnedInterest,
    decimal OriginalInterestEarned,
    decimal ExtensionInterestEarned,
    decimal InterestAdjustments,
    decimal? InterestCollected,
    decimal? InterestReceivable
);

public sealed record InterestEarnedOverviewDto(InterestEarnedSummaryDto Summary, InterestEarnedTotalsDto Totals);

/// <summary>One month's earned-interest figure for the current year vs the same month last year — the "Earned Interest by Month" grouped bar chart.</summary>
public sealed record InterestEarnedMonthlyPointDto(string Month, decimal CurrentYear, decimal PreviousYear);

/// <summary>The Loan Interest Drill-Down (spec §18): every earning period (original + each extension) for one loan, with the same figures shown in the grid row but split out per period.</summary>
public sealed record InterestEarnedPeriodDto(
    string Label,
    string PeriodStart,
    string PeriodEndInclusive,
    int TermDays,
    decimal ContractAmount,
    decimal DailyInterest,
    int EarnedDaysThisPeriod,
    decimal EarnedBeforePeriod,
    decimal EarnedThisPeriod,
    decimal TotalEarned
);

public sealed record InterestEarnedLoanBreakdownDto(
    string LoanId,
    string LoanNumber,
    string CustomerName,
    decimal OriginalContractInterest,
    decimal AdjustedContractInterest,
    decimal InterestAdjustment,
    List<InterestEarnedPeriodDto> Periods
);

/// <summary>Everything the PDF export needs, assembled once by the query handler — the generator itself is a pure Dto -&gt; bytes renderer (spec §26's "must use the same calculations as the screen report, do not recalculate in PDF-generation code").</summary>
public sealed record InterestEarnedReportPdfDto(
    string FromDate,
    string ToDate,
    string GeneratedAt,
    string FiltersSummary,
    InterestEarnedSummaryDto Summary,
    InterestEarnedTotalsDto Totals,
    List<InterestEarnedExportRowDto> Rows
);
