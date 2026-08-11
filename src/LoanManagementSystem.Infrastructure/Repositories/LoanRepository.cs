using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _db;

    public LoanRepository(AppDbContext db)
    {
        _db = db;
    }

    // Tracked (no AsNoTracking) + Extensions/Payments included: this is the
    // path commands use (RecordPayment, ExtendLoan) — they need the full
    // aggregate loaded and tracked so mutating it and calling SaveChanges
    // persists the change.
    public Task<Loan?> GetByIdAsync(LoanId id, CancellationToken ct = default) =>
        _db.Loans
            .Include(l => l.Extensions)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Loan?> GetByIdWithDocumentsAsync(LoanId id, CancellationToken ct = default) =>
        _db.Loans.Include(l => l.Documents).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<List<(LoanDocumentId Id, string OriginalFileName, string ContentType, long FileSizeBytes, DateTime UploadedAtUtc, string UploadedBy)>> GetDocumentsMetadataAsync(
        LoanId loanId, CancellationToken ct = default)
    {
        var rows = await _db.Set<LoanDocument>()
            .AsNoTracking()
            .Where(d => d.LoanId == loanId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new { d.Id, d.OriginalFileName, d.ContentType, d.FileSizeBytes, d.UploadedAtUtc, d.UploadedBy })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.OriginalFileName, r.ContentType, r.FileSizeBytes, r.UploadedAtUtc, r.UploadedBy)).ToList();
    }

    public Task<LoanDocument?> GetDocumentContentAsync(LoanId loanId, LoanDocumentId documentId, CancellationToken ct = default) =>
        _db.Set<LoanDocument>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.LoanId == loanId && d.Id == documentId, ct);

    // Read-only list views don't need the child collections at all — the
    // dashboard table only shows fields that live directly on Loan.
    public Task<List<Loan>> GetAllAsync(CancellationToken ct = default) =>
        _db.Loans.AsNoTracking().OrderByDescending(l => l.CreatedAtUtc).ToListAsync(ct);

    public Task<List<Loan>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default) =>
        _db.Loans.AsNoTracking().Where(l => l.CustomerId == customerId).OrderByDescending(l => l.CreatedAtUtc).ToListAsync(ct);

    public Task<List<Loan>> GetAllWithDetailsAsync(CancellationToken ct = default) =>
        _db.Loans
            .AsNoTracking()
            .Include(l => l.Extensions)
            .Include(l => l.Payments)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<List<(Payment Payment, string CustomerName, int LoanNumber)>> GetRecentPaymentsAsync(int limit, CancellationToken ct = default)
    {
        var rows = await _db.Set<Payment>()
            .AsNoTracking()
            .Join(_db.Loans.AsNoTracking(), p => p.LoanId, l => l.Id, (p, l) => new { Payment = p, l.CustomerId, l.LoanNumber })
            .Join(_db.Customers.AsNoTracking(), x => x.CustomerId, c => c.Id, (x, c) => new { x.Payment, c.FullName, x.LoanNumber })
            .OrderByDescending(x => x.Payment.PaymentDate)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => (r.Payment, r.FullName, r.LoanNumber)).ToList();
    }

    public async Task<Dictionary<CustomerId, int>> GetLoanCountsByCustomerAsync(
        IReadOnlyCollection<CustomerId> customerIds, CancellationToken ct = default)
    {
        if (customerIds.Count == 0) return new Dictionary<CustomerId, int>();

        return await _db.Loans
            .AsNoTracking()
            .Where(l => customerIds.Contains(l.CustomerId))
            .GroupBy(l => l.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<(List<Loan> Items, int TotalCount)> GetPageAsync(
        int pageIndex, int pageSize, string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        string? sortBy, string? sortDir, LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default)
    {
        var query = ApplyFilters(_db.Loans.AsNoTracking().AsQueryable(), search, matchingCustomerIds, filters, asOfDate);

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "loannumber" => desc ? query.OrderByDescending(l => l.LoanNumber) : query.OrderBy(l => l.LoanNumber),
            "principalamount" => desc ? query.OrderByDescending(l => l.PrincipalAmount) : query.OrderBy(l => l.PrincipalAmount),
            "duedate" => desc ? query.OrderByDescending(l => l.DueDate) : query.OrderBy(l => l.DueDate),
            "balance" => desc ? query.OrderByDescending(l => l.Balance) : query.OrderBy(l => l.Balance),
            "status" => desc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            _ => query.OrderByDescending(l => l.CreatedAtUtc),
        };

        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public Task<List<Loan>> GetFilteredAsync(
        string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default) =>
        ApplyFilters(_db.Loans.AsNoTracking().AsQueryable(), search, matchingCustomerIds, filters, asOfDate).ToListAsync(ct);

    /// <summary>
    /// Shared by GetPageAsync (paged list) and GetFilteredAsync (footer
    /// totals over the whole filtered set) so the two never drift out of
    /// sync with each other. Status/OverdueOnly match against DueDate
    /// directly rather than trusting the stored Status column alone — see
    /// the XML doc on ILoanRepository.GetPageAsync for why.
    /// </summary>
    private static IQueryable<Loan> ApplyFilters(
        IQueryable<Loan> query, string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        LoanPageFilters filters, DateOnly asOfDate)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var loanNumber = 0;
            var hasLoanNumberMatch = digits.Length > 0 && int.TryParse(digits, out loanNumber);
            var status = default(LoanStatus);
            var hasStatusMatch = Enum.TryParse(search, ignoreCase: true, out status);

            query = query.Where(l =>
                matchingCustomerIds.Contains(l.CustomerId) ||
                (hasLoanNumberMatch && l.LoanNumber == loanNumber) ||
                (hasStatusMatch && l.Status == status));
        }

        if (filters.Status is { } status2)
        {
            query = status2 == LoanStatus.Overdue
                ? query.Where(l => l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff)
                : query.Where(l => l.Status == status2 && !(l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff));
        }

        if (filters.OverdueOnly)
            query = query.Where(l => l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff);

        if (filters.Classification is { } classification)
            query = query.Where(l => l.Classification == classification);

        if (filters.BadLoansOnly)
            query = query.Where(l => l.Classification == LoanClassification.BadLoan);

        if (filters.LoanDateFrom is { } loanDateFrom)
            query = query.Where(l => l.StartDate >= loanDateFrom);
        if (filters.LoanDateTo is { } loanDateTo)
            query = query.Where(l => l.StartDate <= loanDateTo);

        if (filters.DueDateFrom is { } dueDateFrom)
            query = query.Where(l => l.DueDate >= dueDateFrom);
        if (filters.DueDateTo is { } dueDateTo)
            query = query.Where(l => l.DueDate <= dueDateTo);

        return query;
    }

    public void Add(Loan loan) => _db.Loans.Add(loan);
}
