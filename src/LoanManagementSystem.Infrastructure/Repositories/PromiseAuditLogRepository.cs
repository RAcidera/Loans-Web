using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class PromiseAuditLogRepository : IPromiseAuditLogRepository
{
    private readonly AppDbContext _db;

    public PromiseAuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<PromiseAuditLogEntry>> GetByPromiseIdAsync(PromiseToPayId promiseId, CancellationToken ct = default) =>
        _db.Set<PromiseAuditLogEntry>().AsNoTracking().Where(e => e.PromiseId == promiseId).OrderByDescending(e => e.OccurredAtUtc).ToListAsync(ct);

    public void Add(PromiseAuditLogEntry entry) => _db.Set<PromiseAuditLogEntry>().Add(entry);
}
