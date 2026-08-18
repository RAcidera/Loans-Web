using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class DiaryAuditLogRepository : IDiaryAuditLogRepository
{
    private readonly AppDbContext _db;

    public DiaryAuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<DiaryAuditLogEntry>> GetByDiaryEntryIdAsync(DiaryEntryId diaryEntryId, CancellationToken ct = default) =>
        _db.Set<DiaryAuditLogEntry>().AsNoTracking().Where(e => e.DiaryEntryId == diaryEntryId).OrderByDescending(e => e.OccurredAtUtc).ToListAsync(ct);

    public void Add(DiaryAuditLogEntry entry) => _db.Set<DiaryAuditLogEntry>().Add(entry);
}
