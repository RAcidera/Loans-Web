using LoanManagementSystem.Domain.Diary;

namespace LoanManagementSystem.Domain.Repositories;

public interface IDiaryAuditLogRepository
{
    /// <summary>Newest first — backs the Diary Entry Detail "Audit Information" section (requirements §13).</summary>
    Task<List<DiaryAuditLogEntry>> GetByDiaryEntryIdAsync(DiaryEntryId diaryEntryId, CancellationToken ct = default);

    void Add(DiaryAuditLogEntry entry);
}
