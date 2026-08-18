namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Shaped to match the Angular frontend's DiaryCategory entity field-for-field (requirements §5).</summary>
public sealed record DiaryCategoryDto(string CategoryId, string Name, string Icon, string DisplayColor, bool IsActive, int SortOrder);

/// <summary>Requirements §8/§14 — every field the Diary Entry Detail's "Financial Snapshot" section displays. Immutable once captured (requirements §10); this DTO is a straight read of stored values, never a recalculation.</summary>
public sealed record DiaryFinancialSnapshotDto(
    string SnapshotId,
    string DiaryEntryId,
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
    decimal LoanReleasesMonthToDate,
    string SnapshotDateTime
);

/// <summary>Requirements §4/§13 — one Diary entry, including its category display fields (denormalized here so the Angular timeline never needs a second lookup) and its optional snapshot.</summary>
public sealed record DiaryEntryDto(
    string DiaryEntryId,
    string EntryDate,
    string EntryTime,
    string EntryDateTime,
    string Title,
    string CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryDisplayColor,
    string Notes,
    string Tags,
    string? CustomerId,
    string? CustomerName,
    string? LoanId,
    string? LoanNumber,
    string? ReminderDate,
    string? ReminderTime,
    DiaryFinancialSnapshotDto? Snapshot,
    string CreatedBy,
    string CreatedAt,
    string ModifiedBy,
    string ModifiedAt
);

/// <summary>
/// One row of the Financial Comparison table (requirements §15-17). Key is
/// a stable machine-readable identifier (e.g. "badLoanReceivables") so the
/// Angular comparison-metric component can apply the requirement's
/// contextual coloring rules (§17: bad loans/overdue down = green, cash up
/// = green, gross receivables = neutral) without parsing the display Label.
/// PercentChange is null when SnapshotValue is zero (requirements §16 —
/// rendered as "N/A"/"New" by the frontend, not computed here as either
/// string, since which one applies depends on CurrentValue too).
/// </summary>
public sealed record FinancialComparisonMetricDto(string Key, string Label, decimal SnapshotValue, decimal CurrentValue, decimal Change, decimal? PercentChange);

public sealed record FinancialComparisonDto(string SnapshotDate, string TodayDate, List<FinancialComparisonMetricDto> Metrics);

/// <summary>Backs the Diary Entry Detail "Audit Information" section (requirements §13/§24).</summary>
public sealed record DiaryAuditLogEntryDto(string AuditLogId, string DiaryEntryId, string Action, string Description, string PerformedBy, string OccurredAt);

/// <summary>One row of the sidebar's Category Summary (requirements diary-modern §20) — Count is the total number of diary entries in that category, all-time, independent of the timeline's current filters.</summary>
public sealed record DiaryCategoryCountDto(string CategoryId, string Name, string Icon, string DisplayColor, int Count);

/// <summary>
/// Backs the Diary page's Summary Cards and right-sidebar Quick Summary
/// (requirements diary-modern §5/§20). Every financial figure here comes
/// from IFinancialSnapshotService — the same server-side computation a
/// captured DiaryFinancialSnapshot uses — never recalculated in Angular
/// (requirements §24's financial-integrity rule applies to this card strip
/// too, not just the snapshot feature).
/// </summary>
public sealed record DiarySummaryDto(
    int TotalEntries,
    int EntriesThisMonth,
    int LoanDueCount,
    int PendingRemindersCount,
    decimal CollectionsToday,
    decimal CollectionsMonthToDate,
    decimal LoanReleasesToday,
    decimal LoanReleasesMonthToDate,
    List<DiaryCategoryCountDto> CategoryCounts
);
