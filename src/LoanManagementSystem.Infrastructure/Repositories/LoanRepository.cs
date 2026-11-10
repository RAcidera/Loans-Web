using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _db;
    private readonly IAppDateTimeService _appDateTime;

    public LoanRepository(AppDbContext db, IAppDateTimeService appDateTime)
    {
        _db = db;
        _appDateTime = appDateTime;
    }

    // Tracked (no AsNoTracking) + Extensions/Payments included: this is the
    // path commands use (RecordPayment, ExtendLoan) — they need the full
    // aggregate loaded and tracked so mutating it and calling SaveChanges
    // persists the change.
    public Task<Loan?> GetByIdAsync(LoanId id, CancellationToken ct = default) =>
        _db.Loans
            .Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc))
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

    // Extensions must be included even for these read-only list views: every
    // caller that turns around and calls Loan.RefreshOverdueStatus() on the
    // results (GetLoansQuery, GetLoansPageQuery, GetLoansByCustomerQuery,
    // etc.) relies on _extensions.Count to tell Active apart from Extended —
    // without it, an extended loan's in-memory Status silently reverts to
    // Active on every read (it's still correctly persisted, just clobbered
    // here). Payments is deliberately still left out; nothing on this path
    // needs it.
    public Task<List<Loan>> GetAllAsync(CancellationToken ct = default) =>
        _db.Loans.AsNoTracking().Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc)).OrderByDescending(l => l.CreatedAtUtc).ToListAsync(ct);

    public Task<List<Loan>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default) =>
        _db.Loans.AsNoTracking().Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc)).Where(l => l.CustomerId == customerId).OrderByDescending(l => l.CreatedAtUtc).ToListAsync(ct);

    public Task<List<Loan>> GetAllWithDetailsAsync(CancellationToken ct = default) =>
        _db.Loans
            .AsNoTracking()
            .Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc))
            .Include(l => l.Payments)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)>> GetRecentPaymentsAsync(int limit, CancellationToken ct = default)
    {
        var rows = await _db.Set<Payment>()
            .AsNoTracking()
            .Join(_db.Loans.AsNoTracking(), p => p.LoanId, l => l.Id, (p, l) => new { Payment = p, l.CustomerId, l.LoanNumber })
            .Join(_db.Customers.AsNoTracking(), x => x.CustomerId, c => c.Id, (x, c) => new { x.Payment, x.CustomerId, c.FullName, x.LoanNumber })
            .OrderByDescending(x => x.Payment.PaymentDate)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => (r.Payment, r.CustomerId, r.FullName, r.LoanNumber)).ToList();
    }

    public async Task<(List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)> Items, int TotalCount)> GetPaymentsPageAsync(
        int pageIndex, int pageSize, string? loanSearch, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        DateOnly? dateFrom, DateOnly? dateTo, string? sortBy, string? sortDir, CancellationToken ct = default,
        DateOnly? createdAtFrom = null, DateOnly? createdAtTo = null)
    {
        var query = _db.Set<Payment>()
            .AsNoTracking()
            .Join(_db.Loans.AsNoTracking(), p => p.LoanId, l => l.Id, (p, l) => new { Payment = p, l.CustomerId, l.LoanNumber })
            .Join(_db.Customers.AsNoTracking(), x => x.CustomerId, c => c.Id, (x, c) => new { x.Payment, x.CustomerId, c.FullName, x.LoanNumber });

        if (!string.IsNullOrWhiteSpace(loanSearch))
        {
            var digits = new string(loanSearch.Where(char.IsDigit).ToArray());
            query = digits.Length > 0 && int.TryParse(digits, out var loanNumber)
                ? query.Where(x => x.LoanNumber == loanNumber)
                : query.Where(x => false);
        }

        if (matchingCustomerIds.Count > 0)
            query = query.Where(x => matchingCustomerIds.Contains(x.CustomerId));

        if (dateFrom is { } from)
            query = query.Where(x => x.Payment.PaymentDate >= from);
        if (dateTo is { } to)
            query = query.Where(x => x.Payment.PaymentDate <= to);

        // Payment.CreatedAtUtc is a UTC instant, but createdFrom/createdTo are
        // business-local calendar dates picked in the UI — comparing against
        // naive midnight (as this used to) silently used UTC midnight
        // instead of business midnight, shifting the effective window by the
        // business timezone's offset. GetStartOfBusinessDayUtc converts the
        // business-local day boundary to the correct UTC instant first.
        if (createdAtFrom is { } createdFrom)
            query = query.Where(x => x.Payment.CreatedAtUtc >= _appDateTime.GetStartOfBusinessDayUtc(createdFrom));
        if (createdAtTo is { } createdTo)
            query = query.Where(x => x.Payment.CreatedAtUtc < _appDateTime.GetStartOfBusinessDayUtc(createdTo.AddDays(1)));

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "loan" => desc ? query.OrderByDescending(x => x.LoanNumber) : query.OrderBy(x => x.LoanNumber),
            "customer" => desc ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
            "amount" => desc ? query.OrderByDescending(x => x.Payment.AmountPaid) : query.OrderBy(x => x.Payment.AmountPaid),
            "createdat" => desc ? query.OrderByDescending(x => x.Payment.CreatedAtUtc) : query.OrderBy(x => x.Payment.CreatedAtUtc),
            "method" => desc ? query.OrderByDescending(x => x.Payment.PaymentMethod) : query.OrderBy(x => x.Payment.PaymentMethod),
            "reference" => desc ? query.OrderByDescending(x => x.Payment.ReferenceNumber) : query.OrderBy(x => x.Payment.ReferenceNumber),
            "notes" => desc ? query.OrderByDescending(x => x.Payment.Notes) : query.OrderBy(x => x.Payment.Notes),
            "date" when !desc => query.OrderBy(x => x.Payment.PaymentDate),
            _ => query.OrderByDescending(x => x.Payment.PaymentDate),
        };
        // Stable paging when many payments share the sort key (e.g. method).
        sorted = sorted.ThenByDescending(x => x.Payment.CreatedAtUtc);

        var rows = await sorted
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (rows.Select(r => (r.Payment, r.CustomerId, r.FullName, r.LoanNumber)).ToList(), totalCount);
    }

    public async Task<List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)>> GetPaymentsFilteredAsync(
        string? loanSearch, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default,
        DateOnly? createdAtFrom = null, DateOnly? createdAtTo = null)
    {
        var query = _db.Set<Payment>()
            .AsNoTracking()
            .Join(_db.Loans.AsNoTracking(), p => p.LoanId, l => l.Id, (p, l) => new { Payment = p, l.CustomerId, l.LoanNumber })
            .Join(_db.Customers.AsNoTracking(), x => x.CustomerId, c => c.Id, (x, c) => new { x.Payment, x.CustomerId, c.FullName, x.LoanNumber });

        if (!string.IsNullOrWhiteSpace(loanSearch))
        {
            var digits = new string(loanSearch.Where(char.IsDigit).ToArray());
            query = digits.Length > 0 && int.TryParse(digits, out var loanNumber)
                ? query.Where(x => x.LoanNumber == loanNumber)
                : query.Where(x => false);
        }

        if (matchingCustomerIds.Count > 0)
            query = query.Where(x => matchingCustomerIds.Contains(x.CustomerId));

        if (dateFrom is { } from)
            query = query.Where(x => x.Payment.PaymentDate >= from);
        if (dateTo is { } to)
            query = query.Where(x => x.Payment.PaymentDate <= to);

        // Payment.CreatedAtUtc is a UTC instant, but createdFrom/createdTo are
        // business-local calendar dates picked in the UI — comparing against
        // naive midnight (as this used to) silently used UTC midnight
        // instead of business midnight, shifting the effective window by the
        // business timezone's offset. GetStartOfBusinessDayUtc converts the
        // business-local day boundary to the correct UTC instant first.
        if (createdAtFrom is { } createdFrom)
            query = query.Where(x => x.Payment.CreatedAtUtc >= _appDateTime.GetStartOfBusinessDayUtc(createdFrom));
        if (createdAtTo is { } createdTo)
            query = query.Where(x => x.Payment.CreatedAtUtc < _appDateTime.GetStartOfBusinessDayUtc(createdTo.AddDays(1)));

        var rows = await query.ToListAsync(ct);
        return rows.Select(r => (r.Payment, r.CustomerId, r.FullName, r.LoanNumber)).ToList();
    }

    public async Task<(List<(Payment Payment, int LoanNumber)> Items, int TotalCount)> GetPaymentsByCustomerPageAsync(
        CustomerId customerId, int pageIndex, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default)
    {
        var query = _db.Set<Payment>()
            .AsNoTracking()
            .Join(_db.Loans.AsNoTracking().Where(l => l.CustomerId == customerId), p => p.LoanId, l => l.Id, (p, l) => new { Payment = p, l.LoanNumber });

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "amount" => desc ? query.OrderByDescending(x => x.Payment.AmountPaid) : query.OrderBy(x => x.Payment.AmountPaid),
            "loan" => desc ? query.OrderByDescending(x => x.LoanNumber) : query.OrderBy(x => x.LoanNumber),
            "date" when !desc => query.OrderBy(x => x.Payment.PaymentDate),
            _ => query.OrderByDescending(x => x.Payment.PaymentDate),
        };

        var rows = await sorted
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (rows.Select(r => (r.Payment, r.LoanNumber)).ToList(), totalCount);
    }

    public async Task<(List<Payment> Items, int TotalCount)> GetPaymentsByLoanPageAsync(
        LoanId loanId, int pageIndex, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default)
    {
        var query = _db.Set<Payment>().AsNoTracking().Where(p => p.LoanId == loanId);

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "date" when !desc => query.OrderBy(p => p.PaymentDate),
            "date" => query.OrderByDescending(p => p.PaymentDate),
            _ => query.OrderByDescending(p => p.PaymentDate),
        };

        var items = await sorted
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Dictionary<CustomerId, (int LoanCount, decimal OutstandingBalance)>> GetLoanCountsAndBalanceByCustomerAsync(
        IReadOnlyCollection<CustomerId> customerIds, CancellationToken ct = default)
    {
        if (customerIds.Count == 0) return new Dictionary<CustomerId, (int, decimal)>();

        // Grouping by CustomerId with two aggregates (Count + Sum over the
        // Balance value object's .Amount) in the same projection fails to
        // translate when combined with the OPENJSON-based Contains(customerIds)
        // filter EF Core 8 uses for a parameterized collection — "could not
        // be translated" at runtime, not a compile-time error. Projecting
        // the flat rows first and aggregating client-side avoids it; this
        // is scoped to one page's worth of customer IDs, so the row count
        // is small regardless of how many loans exist overall.
        var rows = await _db.Loans
            .AsNoTracking()
            .Where(l => customerIds.Contains(l.CustomerId))
            .Select(l => new { l.CustomerId, Amount = l.Balance.Amount })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.CustomerId)
            .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(r => r.Amount)));
    }

    public async Task<Dictionary<CustomerId, CustomerLoanStats>> GetCustomerLoanStatsAsync(
        IReadOnlyCollection<CustomerId>? customerIds, DateOnly asOfDate, CancellationToken ct = default)
    {
        if (customerIds is { Count: 0 }) return new Dictionary<CustomerId, CustomerLoanStats>();

        var loans = _db.Loans.AsNoTracking();
        if (customerIds is not null)
            loans = loans.Where(l => customerIds.Contains(l.CustomerId));

        // Flat rows aggregated client-side — same reason as
        // GetLoanCountsAndBalanceByCustomerAsync: grouped aggregates over the
        // value-converted Money columns (combined with the OPENJSON Contains
        // filter) don't translate on SQL Server. Scoped to one page of
        // customers on the normal path; unscoped only when sorting by one of
        // these figures, which is a few columns per loan.
        var loanRows = await loans
            .Select(l => new { l.CustomerId, l.Balance, l.DueDate, l.Status })
            .ToListAsync(ct);

        var paymentRows = await _db.Set<Payment>()
            .AsNoTracking()
            .Join(loans, p => p.LoanId, l => l.Id, (p, l) => new { l.CustomerId, p.PaymentDate })
            .ToListAsync(ct);

        var lastPayment = paymentRows
            .GroupBy(r => r.CustomerId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.PaymentDate));

        return loanRows
            .GroupBy(r => r.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => new CustomerLoanStats(
                    LoanCount: g.Count(),
                    OutstandingBalance: g.Sum(r => r.Balance.Amount),
                    // Same rule as ApplyFilters' Overdue match.
                    OverdueLoanCount: g.Count(r => r.DueDate < asOfDate && r.Status != LoanStatus.Paid && r.Status != LoanStatus.WrittenOff),
                    LastPaymentDate: lastPayment.TryGetValue(g.Key, out var d) ? d : null));
    }

    public async Task<(List<Loan> Items, int TotalCount)> GetPageAsync(
        int pageIndex, int pageSize, string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        string? sortBy, string? sortDir, LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default)
    {
        var query = ApplyFilters(_db.Loans.AsNoTracking().Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc)).AsQueryable(), search, matchingCustomerIds, filters, asOfDate);

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "loannumber" => desc ? query.OrderByDescending(l => l.LoanNumber) : query.OrderBy(l => l.LoanNumber),
            "customer" => OrderByCustomerName(query, desc),
            "loandate" => desc ? query.OrderByDescending(l => l.StartDate) : query.OrderBy(l => l.StartDate),
            "principalamount" => desc ? query.OrderByDescending(l => l.PrincipalAmount) : query.OrderBy(l => l.PrincipalAmount),
            "duedate" => desc ? query.OrderByDescending(l => l.DueDate) : query.OrderBy(l => l.DueDate),
            "balance" => desc ? query.OrderByDescending(l => l.Balance) : query.OrderBy(l => l.Balance),
            "interest" => desc ? query.OrderByDescending(l => l.TotalInterest) : query.OrderBy(l => l.TotalInterest),
            "extensioncharges" => desc ? query.OrderByDescending(l => l.TotalExtensionCharges) : query.OrderBy(l => l.TotalExtensionCharges),
            "totalpaid" => desc ? query.OrderByDescending(l => l.TotalPaid) : query.OrderBy(l => l.TotalPaid),
            "classification" => desc ? query.OrderByDescending(l => l.Classification) : query.OrderBy(l => l.Classification),
            // Sort by the EFFECTIVE status the grid shows: "Overdue" is never
            // stored (see ApplyFilters), so a past-due open loan sorts as
            // Overdue rather than with Active/Extended.
            "status" => desc
                ? query.OrderByDescending(l => l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff ? LoanStatus.Overdue : l.Status)
                : query.OrderBy(l => l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff ? LoanStatus.Overdue : l.Status),
            _ => query.OrderByDescending(l => l.CreatedAtUtc),
        };
        // Stable paging when many rows share the sort key (e.g. status).
        if (sortBy is not null)
            query = ((IOrderedQueryable<Loan>)query).ThenByDescending(l => l.LoanNumber);

        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public Task<List<Loan>> GetFilteredAsync(
        string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default) =>
        ApplyFilters(_db.Loans.AsNoTracking().Include(l => l.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc)).AsQueryable(), search, matchingCustomerIds, filters, asOfDate).ToListAsync(ct);

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

        if (filters.Statuses is { Count: > 0 } statuses)
        {
            // "Overdue" is never a value actually stored in the Status
            // column at filter time (see this method's doc comment) — it
            // has to be matched via DueDate directly, in an OR alongside
            // whichever other selected statuses match the stored column.
            var wantsOverdue = statuses.Contains(LoanStatus.Overdue);
            var otherStatuses = statuses.Where(s => s != LoanStatus.Overdue).ToList();

            query = query.Where(l =>
                (wantsOverdue && l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff) ||
                (otherStatuses.Contains(l.Status) && !(l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff)));
        }

        if (filters.OverdueOnly)
            query = query.Where(l => l.DueDate < asOfDate && l.Status != LoanStatus.Paid && l.Status != LoanStatus.WrittenOff);

        if (filters.Classifications is { Count: > 0 } classifications)
            query = query.Where(l => classifications.Contains(l.Classification));

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

    /// <summary>
    /// Loan and Customer have no EF navigation property between them (see
    /// ILoanRepository.GetPageAsync's doc comment), but they're still the
    /// same DbContext — a correlated subquery for the sort key, rather than
    /// a Join+Select, keeps this an IQueryable&lt;Loan&gt; all the way
    /// through (a Join followed by Select-back-to-Loan risks EF Core
    /// silently dropping the caller's .Include(Extensions), since Include
    /// metadata is tied to the query projecting the entity directly).
    /// </summary>
    private IQueryable<Loan> OrderByCustomerName(IQueryable<Loan> query, bool desc)
    {
        return desc
            ? query.OrderByDescending(l => _db.Customers.Where(c => c.Id == l.CustomerId).Select(c => c.FullName).FirstOrDefault())
            : query.OrderBy(l => _db.Customers.Where(c => c.Id == l.CustomerId).Select(c => c.FullName).FirstOrDefault());
    }

    public void Add(Loan loan) => _db.Loans.Add(loan);
}
