namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// The Dashboard's "Monthly Capital Growth" panel — one business-local month
/// per Series item, oldest first, ending with the current (month-to-date)
/// month. HistoryBasis is always "restated": ledger edits/deletes revise rows
/// in place, so past months are recomputed from today's records rather than
/// replayed audit-exact. The selected-month breakdown is just one Series item,
/// not a separate payload.
/// </summary>
public sealed record MonthlyCapitalGrowthDto(
    string TimeZone,
    DateTime AsOfUtc,
    string AsOfDate,
    string Currency,
    int Months,
    string HistoryBasis,
    string? HistoryStartDate,
    List<CapitalGrowthMonthDto> Series,
    CapitalGrowthSummaryDto Summary,
    CapitalGrowthReconciliationDto Reconciliation,
    List<CapitalGrowthWarningDto> Warnings
);

/// <summary>Status: "complete" | "partialmonth" (current month, as of today) | "partialhistory" (records begin mid-month) | "nohistory" (all amounts null — unavailable, not zero).</summary>
public sealed record CapitalGrowthMonthDto(
    string MonthKey,
    string StartDate,
    string EndCutoffDate,
    bool IsPartialMonth,
    string Status,
    decimal? BeginningCash,
    decimal? EndingCash,
    decimal? BeginningGrossReceivables,
    decimal? EndingGrossReceivables,
    decimal? BeginningPosition,
    decimal? EndingPosition,
    decimal? OwnerWithdrawals,
    decimal? OwnerDeposits,
    decimal? PositionChange,
    decimal? AdjustedGrowth,
    decimal? GrowthPercent
);

/// <summary>TotalGrowth/AverageGrowthIncludingCurrentMonth cover completed months plus the current month-to-date; AverageCompletedMonthGrowth and BestMonth cover completed months only.</summary>
public sealed record CapitalGrowthSummaryDto(
    decimal? TotalGrowth,
    int MonthsInTotal,
    decimal? AverageCompletedMonthGrowth,
    int CompletedMonthCount,
    decimal? AverageGrowthIncludingCurrentMonth,
    string? BestMonthKey,
    decimal? BestMonthGrowth,
    decimal? PreviousPeriodGrowth,
    decimal? ChangeVsPreviousPeriodPercent
);

/// <summary>DashboardPosition is the live KPI formula (all cash entries + every loan's Balance) on the same data — the current month's EndingPosition should equal it unless future-dated entries exist.</summary>
public sealed record CapitalGrowthReconciliationDto(
    decimal DashboardPosition,
    decimal? CurrentEndingPosition,
    decimal? Difference,
    bool Matches,
    List<SnapshotReconciliationDto> Snapshots
);

/// <summary>A Diary Financial Snapshot vs. the reconstructed position at the end of its business date. Differences are expected after later backdated corrections; they're reported, never applied.</summary>
public sealed record SnapshotReconciliationDto(
    DateTime SnapshotDateTimeUtc,
    string BusinessDate,
    decimal SnapshotPosition,
    decimal ReconstructedPosition,
    decimal Difference
);

public sealed record CapitalGrowthWarningDto(string Code, string Message, List<string> LoanNumbers);
