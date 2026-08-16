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

    // Tracked (no AsNoTracking): PaymentEditedEventHandler mutates this
    // entry via ReviseForPaymentEdit and needs SaveChanges to persist it.
    public Task<LoanLedgerEntry?> GetByPaymentReferenceAsync(LoanId loanId, PaymentId paymentId, CancellationToken ct = default) =>
        _db.LoanLedgerEntries.FirstOrDefaultAsync(e => e.LoanId == loanId && e.ReferenceId == paymentId.ToString(), ct);

    // Tracked: PaymentDeletedEventHandler/LoanExtensionDeletedEventHandler
    // remove this exact row via Remove(), same reasoning as GetByPaymentReferenceAsync.
    public Task<LoanLedgerEntry?> GetByReferenceIdAsync(LoanId loanId, string referenceId, CancellationToken ct = default) =>
        _db.LoanLedgerEntries.FirstOrDefaultAsync(e => e.LoanId == loanId && e.ReferenceId == referenceId, ct);

    // Tracked: the delete handlers call ShiftRunningBalance() on each of
    // these and need SaveChanges to persist it.
    public Task<List<LoanLedgerEntry>> GetAfterAsync(LoanId loanId, DateTime createdAtUtc, CancellationToken ct = default) =>
        _db.LoanLedgerEntries.Where(e => e.LoanId == loanId && e.CreatedAtUtc > createdAtUtc).ToListAsync(ct);

    public void Add(LoanLedgerEntry entry) => _db.LoanLedgerEntries.Add(entry);

    public void Remove(LoanLedgerEntry entry) => _db.LoanLedgerEntries.Remove(entry);
}
