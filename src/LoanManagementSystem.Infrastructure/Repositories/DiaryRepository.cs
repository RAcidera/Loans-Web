using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class DiaryRepository : IDiaryRepository
{
    private readonly AppDbContext _db;

    public DiaryRepository(AppDbContext db)
    {
        _db = db;
    }

    // Tracked (no AsNoTracking) + Snapshot included: this is the path every
    // caller uses, including Update/Delete commands, which need the
    // aggregate loaded and tracked so mutating it and calling SaveChanges
    // persists the change.
    public Task<DiaryEntry?> GetByIdAsync(DiaryEntryId id, CancellationToken ct = default) =>
        _db.Set<DiaryEntry>().Include(e => e.Snapshot).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<DiaryEntry>> SearchAsync(DiarySearchFilters filters, CancellationToken ct = default) =>
        ApplyFilters(_db.Set<DiaryEntry>().AsNoTracking().Include(e => e.Snapshot).AsQueryable(), filters)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.EntryTime)
            .ToListAsync(ct);

    private static IQueryable<DiaryEntry> ApplyFilters(IQueryable<DiaryEntry> query, DiarySearchFilters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.SearchText))
        {
            var text = filters.SearchText;
            var matchingCustomerIds = filters.MatchingCustomerIds ?? Array.Empty<Domain.Customers.CustomerId>();
            var matchingLoanIds = filters.MatchingLoanIds ?? Array.Empty<Domain.Loans.LoanId>();

            query = query.Where(e =>
                e.Title.Contains(text) || e.Notes.Contains(text) || e.Tags.Contains(text) ||
                (e.CustomerId.HasValue && matchingCustomerIds.Contains(e.CustomerId.Value)) ||
                (e.LoanId.HasValue && matchingLoanIds.Contains(e.LoanId.Value)));
        }

        if (filters.CategoryId is { } categoryId)
            query = query.Where(e => e.CategoryId == categoryId);

        if (filters.DateFrom is { } dateFrom)
            query = query.Where(e => e.EntryDate >= dateFrom);
        if (filters.DateTo is { } dateTo)
            query = query.Where(e => e.EntryDate <= dateTo);

        if (filters.CustomerId is { } customerId)
            query = query.Where(e => e.CustomerId == customerId);

        if (filters.LoanId is { } loanId)
            query = query.Where(e => e.LoanId == loanId);

        if (filters.HasFinancialSnapshot is { } hasSnapshot)
            query = hasSnapshot ? query.Where(e => e.Snapshot != null) : query.Where(e => e.Snapshot == null);

        if (filters.HasReminder is { } hasReminder)
            query = hasReminder ? query.Where(e => e.ReminderDate != null) : query.Where(e => e.ReminderDate == null);

        return query;
    }

    public Task<List<DiaryEntry>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.Set<DiaryEntry>()
            .AsNoTracking()
            .Where(e => (e.EntryDate >= from && e.EntryDate <= to) || (e.ReminderDate != null && e.ReminderDate >= from && e.ReminderDate <= to))
            .ToListAsync(ct);

    public void Add(DiaryEntry entry) => _db.Set<DiaryEntry>().Add(entry);

    public void Remove(DiaryEntry entry) => _db.Set<DiaryEntry>().Remove(entry);
}
