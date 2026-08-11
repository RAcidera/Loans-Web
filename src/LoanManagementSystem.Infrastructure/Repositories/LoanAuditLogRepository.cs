using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class LoanAuditLogRepository : ILoanAuditLogRepository
{
    private readonly AppDbContext _db;

    public LoanAuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<LoanAuditLogEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default) =>
        _db.LoanAuditLogEntries.AsNoTracking().Where(e => e.LoanId == loanId).ToListAsync(ct);

    public void Add(LoanAuditLogEntry entry) => _db.LoanAuditLogEntries.Add(entry);
}
