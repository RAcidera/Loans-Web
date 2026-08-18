using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary;

/// <summary>
/// Child entity of the DiaryEntry aggregate (requirements §8) — captured
/// once, at creation, via DiaryEntry.AttachSnapshot(), and never mutated
/// afterward. There is deliberately no setter or Edit method anywhere on
/// this type: requirements §10 ("Snapshot Must Be Immutable") is a domain
/// invariant, not just a UI convention, so it's enforced by this class
/// having no mutation surface at all rather than by callers agreeing not
/// to call one.
/// </summary>
public class DiaryFinancialSnapshot : Entity<DiaryFinancialSnapshotId>
{
    public DiaryEntryId DiaryEntryId { get; private set; }
    public decimal GrossReceivables { get; private set; }
    public decimal CollectibleReceivables { get; private set; }
    public decimal BadLoanReceivables { get; private set; }
    public decimal CashOnHand { get; private set; }
    public int ActiveLoanCount { get; private set; }
    public int OverdueLoanCount { get; private set; }
    public int BadLoanCount { get; private set; }
    public decimal CollectionsToday { get; private set; }
    public decimal CollectionsMonthToDate { get; private set; }
    public decimal LoanReleasesToday { get; private set; }
    public decimal LoanReleasesMonthToDate { get; private set; }
    public DateTime SnapshotDateTimeUtc { get; private set; }

    private DiaryFinancialSnapshot() { } // EF Core

    internal DiaryFinancialSnapshot(
        DiaryEntryId diaryEntryId, decimal grossReceivables, decimal collectibleReceivables, decimal badLoanReceivables, decimal cashOnHand,
        int activeLoanCount, int overdueLoanCount, int badLoanCount,
        decimal collectionsToday, decimal collectionsMonthToDate, decimal loanReleasesToday, decimal loanReleasesMonthToDate)
        : base(DiaryFinancialSnapshotId.New())
    {
        DiaryEntryId = diaryEntryId;
        GrossReceivables = grossReceivables;
        CollectibleReceivables = collectibleReceivables;
        BadLoanReceivables = badLoanReceivables;
        CashOnHand = cashOnHand;
        ActiveLoanCount = activeLoanCount;
        OverdueLoanCount = overdueLoanCount;
        BadLoanCount = badLoanCount;
        CollectionsToday = collectionsToday;
        CollectionsMonthToDate = collectionsMonthToDate;
        LoanReleasesToday = loanReleasesToday;
        LoanReleasesMonthToDate = loanReleasesMonthToDate;
        SnapshotDateTimeUtc = DateTime.UtcNow;
    }
}
