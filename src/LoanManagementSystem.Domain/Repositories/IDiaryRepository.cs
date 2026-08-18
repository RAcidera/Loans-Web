using LoanManagementSystem.Domain.Diary;

namespace LoanManagementSystem.Domain.Repositories;

public interface IDiaryRepository
{
    Task<DiaryEntry?> GetByIdAsync(DiaryEntryId id, CancellationToken ct = default);

    /// <summary>Every diary entry matching the given filters (requirements §12), sorted EntryDateTime DESC (requirements §11) — the timeline is not paged, matching its "chronological timeline" framing rather than a data grid.</summary>
    Task<List<DiaryEntry>> SearchAsync(DiarySearchFilters filters, CancellationToken ct = default);

    /// <summary>Every diary entry whose EntryDate OR ReminderDate falls within [from, to] — backs the Calendar's "Diary Entries"/"Follow-up Reminders" event sources (requirements §19), which can land on different days for the same entry.</summary>
    Task<List<DiaryEntry>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    void Add(DiaryEntry entry);

    void Remove(DiaryEntry entry);
}
