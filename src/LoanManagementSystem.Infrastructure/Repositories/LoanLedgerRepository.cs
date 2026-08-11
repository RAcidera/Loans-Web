using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class LoanLedgerRepository : ILoanLedgerRepository
{
    private readonly AppDbContext _db;

    public LoanLedgerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<LoanLedgerEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default) =>
        _db.LoanLedgerEntries.AsNoTracking().Where(e => e.LoanId == loanId).ToListAsync(ct);

    public Task<bool> AnyForLoanAsync(LoanId loanId, CancellationToken ct = default) =>
        _db.LoanLedgerEntries.AsNoTracking().AnyAsync(e => e.LoanId == loanId, ct);

    public void Add(LoanLedgerEntry entry) => _db.LoanLedgerEntries.Add(entry);
}
