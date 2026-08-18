using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class DiaryCategoryRepository : IDiaryCategoryRepository
{
    private readonly AppDbContext _db;

    public DiaryCategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<DiaryCategory?> GetByIdAsync(DiaryCategoryId id, CancellationToken ct = default) =>
        _db.Set<DiaryCategory>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<DiaryCategory>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Set<DiaryCategory>().AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);

    public Task<List<DiaryCategory>> GetAllAsync(CancellationToken ct = default) =>
        _db.Set<DiaryCategory>().AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);

    public void Add(DiaryCategory category) => _db.Set<DiaryCategory>().Add(category);
}
