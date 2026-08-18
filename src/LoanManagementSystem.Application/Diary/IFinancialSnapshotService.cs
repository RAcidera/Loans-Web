namespace LoanManagementSystem.Application.Diary;

/// <summary>
/// Requirements §21's "Financial snapshot service" — CaptureCurrentSnapshot
/// and CompareSnapshotToCurrent are handled directly by their own
/// command/query handlers (CreateDiaryEntryCommandHandler,
/// CompareSnapshotToCurrentQueryHandler), each calling
/// GetCurrentFinancialPositionAsync below and doing something different
/// with the result (persist vs. diff-against-stored). This interface exists
/// so both call the exact same computation — see FinancialSnapshotService's
/// doc comment for why it, in turn, only calls FinancialCalculations
/// rather than re-deriving any formula itself.
/// </summary>
public interface IFinancialSnapshotService
{
    Task<FinancialPositionSnapshot> GetCurrentFinancialPositionAsync(DateOnly asOfDate, CancellationToken ct = default);

    /// <summary>Collections received / loan principal released strictly within [from, to] — used by CompareSnapshotToCurrent's "Since This Snapshot" summary (requirements diary-detail-snapshot: activity between a captured snapshot and today, distinct from GetCurrentFinancialPositionAsync's calendar-month-to-date figures).</summary>
    Task<(decimal Collections, decimal LoanReleases)> GetActivitySinceAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

/// <summary>Every figure requirements §8's DiaryFinancialSnapshot table stores, computed live — see FinancialSnapshotService.</summary>
public sealed record FinancialPositionSnapshot(
    decimal GrossReceivables,
    decimal CollectibleReceivables,
    decimal BadLoanReceivables,
    decimal CashOnHand,
    int ActiveLoanCount,
    int OverdueLoanCount,
    int BadLoanCount,
    decimal CollectionsToday,
    decimal CollectionsMonthToDate,
    decimal LoanReleasesToday,
    decimal LoanReleasesMonthToDate
);
