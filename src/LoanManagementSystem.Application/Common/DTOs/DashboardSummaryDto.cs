namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Backs the Dashboard's "Collections Overview"/"Collections (Past 7 Days)"/
/// "Receivables Breakdown" charts, month-over-month trend badges on the
/// top KPI row, and the "Recent Loans" widget — everything on that page
/// beyond Cash on Hand (GetCashSummaryQuery) and Recent Payments
/// (GetRecentPaymentsQuery), which already had their own endpoints.
/// </summary>
public sealed record DashboardSummaryDto(
    decimal GrossReceivables,
    decimal? GrossReceivablesChangePercent,
    decimal CollectibleReceivables,
    decimal? CollectibleReceivablesChangePercent,
    decimal BadLoanReceivables,
    decimal? BadLoanReceivablesChangePercent,
    int ActiveLoansCount,
    decimal? ActiveLoansChangePercent,
    int OverdueLoansCount,
    decimal? OverdueLoansChangePercent,
    int LoansDueThisWeekCount,
    List<MonthlyCollectionDto> MonthlyCollections,
    List<DailyCollectionDto> Last7DaysCollections,
    ReceivablesBreakdownDto ReceivablesBreakdown,
    List<LoanDto> RecentLoans
);

/// <summary>One calendar month's collected-payments total, this year vs. the same month last year — "Collections Overview" bar chart.</summary>
public sealed record MonthlyCollectionDto(string Month, decimal ThisYear, decimal LastYear);

/// <summary>One day's collected-payments total — "Collections (Past 7 Days)" line chart.</summary>
public sealed record DailyCollectionDto(string Date, decimal Amount);

/// <summary>
/// Current/Overdue/BadLoan are each loan's outstanding Balance (the same
/// figure Gross Receivables sums), so those three always add up to exactly
/// the Gross Receivables KPI — this is that same total, just split by
/// disposition. Paid is the exception: a fully paid loan's Balance is
/// always zero, so it uses TotalPaid instead, showing how much was
/// collected on already-settled loans alongside the three outstanding
/// buckets rather than omitting them from the chart entirely.
/// </summary>
public sealed record ReceivablesBreakdownDto(decimal Current, decimal Overdue, decimal BadLoan, decimal Paid);
