using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class CashLedgerRepository : ICashLedgerRepository
{
    private readonly AppDbContext _db;

    public CashLedgerRepository(AppDbContext db)
    {
        _db = db;
    }

    // Not AsNoTracking: GetCashSummaryQuery reduces over every entry on
    // every request, and the ledger for a small lending business stays
    // small (thousands, not millions, of rows) — simplicity here beats a
    // premature SUM-in-SQL optimization. Revisit with a dedicated
    // aggregate query (or a materialized summary table updated by the
    // event handlers) if the ledger ever grows large enough for this to matter.
    public Task<List<CashLedgerEntry>> GetAllAsync(CancellationToken ct = default) =>
        _db.CashLedgerEntries.AsNoTracking().ToListAsync(ct);

    public void Add(CashLedgerEntry entry) => _db.CashLedgerEntries.Add(entry);
}
