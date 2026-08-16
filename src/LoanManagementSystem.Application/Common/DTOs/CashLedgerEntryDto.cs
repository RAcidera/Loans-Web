namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record CashLedgerEntryDto(
    string LedgerId,
    string TransactionDate,
    string TransactionType,
    string? ReferenceId,
    decimal Amount,
    /// <summary>Whether Amount is Cash In or Cash Out — required on the wire because Adjustment's direction isn't derivable from TransactionType alone.</summary>
    bool IsCashIn,
    /// <summary>System-generated (loan_release/payment_received) rows can't be edited or deleted from this ledger — the Cash Transactions grid's row menu only offers Edit/Delete when this is false.</summary>
    bool IsAutomatic,
    /// <summary>Cash on Hand immediately after this entry, computed over the full chronological ledger. Null on rows where the caller didn't compute one (e.g. the Add/Edit response).</summary>
    decimal? RunningBalance,
    string Remarks,
    string CreatedAt
);

/// <summary>Footer totals for a filtered/date-ranged slice of the ledger — GetCashLedgerTotalsQuery, paired with GetCashLedgerPageQuery's same filters.</summary>
public sealed record CashLedgerTotalsDto(decimal CashIn, decimal CashOut, decimal NetChange, int Count);

/// <summary>
/// Matches Angular's CashSummary. Redesigned per the Cash Ledger UX review:
/// one Cash on Hand figure up top, with This Month's Cash In/Cash Out/Net
/// Change as secondary context underneath (why the total moved), each with
/// its own vs-last-month trend. Total revolving funds and outstanding
/// principal were deliberately dropped from this page — they're loan/dashboard
/// concerns, not "what cash do I have and what caused it to change" ones.
/// </summary>
public sealed record CashSummaryDto(
    decimal CashOnHand,
    string AsOfDate,
    decimal CashInThisMonth,
    decimal CashOutThisMonth,
    decimal NetChangeThisMonth,
    /// <summary>Exact, not an approximation — the cash ledger is an append-only, dated timeline, so "as of a month ago" is a real historical sum, not a same-day proxy over a different cohort. Null when there's nothing a month old to compare against.</summary>
    decimal? CashOnHandChangePercent,
    decimal? CashInChangePercent,
    decimal? CashOutChangePercent,
    decimal? NetChangePercent
);
