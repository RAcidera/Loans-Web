using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;
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

    // Tracked (no AsNoTracking): PaymentEditedEventHandler needs to mutate
    // this entry via Revise() and have SaveChanges persist it.
    public Task<CashLedgerEntry?> GetBySourcePaymentIdAsync(PaymentId paymentId, CancellationToken ct = default) =>
        _db.CashLedgerEntries.FirstOrDefaultAsync(e => e.SourcePaymentId == paymentId, ct);

    // Tracked, same reason: LoanOriginationEditedEventHandler mutates this via Revise().
    public Task<CashLedgerEntry?> GetLoanReleaseEntryAsync(string loanNumber, CancellationToken ct = default) =>
        _db.CashLedgerEntries.FirstOrDefaultAsync(e => e.TransactionType == CashTransactionType.LoanRelease && e.ReferenceId == loanNumber, ct);

    // Tracked: EditCashTransactionCommand/DeleteCashTransactionCommand mutate/remove this via EditManual()/Remove() and rely on SaveChanges.
    public Task<CashLedgerEntry?> GetByIdAsync(CashLedgerEntryId id, CancellationToken ct = default) =>
        _db.CashLedgerEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    // Not AsNoTracking: GetCashSummaryQuery reduces over every entry on
    // every request, and the ledger for a small lending business stays
    // small (thousands, not millions, of rows) — simplicity here beats a
    // premature SUM-in-SQL optimization. Revisit with a dedicated
    // aggregate query (or a materialized summary table updated by the
    // event handlers) if the ledger ever grows large enough for this to matter.
    public Task<List<CashLedgerEntry>> GetAllAsync(CancellationToken ct = default) =>
        _db.CashLedgerEntries.AsNoTracking().ToListAsync(ct);

    public void Add(CashLedgerEntry entry) => _db.CashLedgerEntries.Add(entry);

    public void Remove(CashLedgerEntry entry) => _db.CashLedgerEntries.Remove(entry);
}
