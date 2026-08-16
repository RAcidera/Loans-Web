using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByIdWithDocumentsAsync(CustomerId id, CancellationToken ct = default) =>
        _db.Customers.Include(c => c.Documents).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<(CustomerDocumentId Id, string OriginalFileName, string ContentType, long FileSizeBytes, DateTime UploadedAtUtc, string UploadedBy)>> GetDocumentsMetadataAsync(
        CustomerId customerId, CancellationToken ct = default)
    {
        // Projects every column except Content, so the BLOB never leaves SQL Server for a list view.
        var rows = await _db.Set<CustomerDocument>()
            .AsNoTracking()
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new { d.Id, d.OriginalFileName, d.ContentType, d.FileSizeBytes, d.UploadedAtUtc, d.UploadedBy })
            .ToListAsync(ct);

        return rows.Select(r => (r.Id, r.OriginalFileName, r.ContentType, r.FileSizeBytes, r.UploadedAtUtc, r.UploadedBy)).ToList();
    }

    public Task<CustomerDocument?> GetDocumentContentAsync(CustomerId customerId, CustomerDocumentId documentId, CancellationToken ct = default) =>
        _db.Set<CustomerDocument>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CustomerId == customerId && d.Id == documentId, ct);

    // AsNoTracking: this is a pure read path (list views), so there's no
    // reason to pay EF's change-tracking overhead or risk an unrelated
    // later SaveChanges accidentally persisting a mutation made in memory.
    public Task<List<Customer>> GetAllAsync(CancellationToken ct = default) =>
        _db.Customers.AsNoTracking().OrderBy(c => c.FullName).ToListAsync(ct);

    public async Task<(List<Customer> Items, int TotalCount)> GetPageAsync(
        int pageIndex, int pageSize, string? search, string? sortBy, string? sortDir,
        CustomerStatus? status = null, CancellationToken ct = default)
    {
        var query = ApplyFilters(_db.Customers.AsNoTracking().AsQueryable(), search, status);

        var totalCount = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "borrowertype" => desc ? query.OrderByDescending(c => c.BorrowerType) : query.OrderBy(c => c.BorrowerType),
            _ => desc ? query.OrderByDescending(c => c.FullName) : query.OrderBy(c => c.FullName),
        };

        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public Task<List<Customer>> GetFilteredAsync(string? search, CustomerStatus? status, CancellationToken ct = default) =>
        ApplyFilters(_db.Customers.AsNoTracking().AsQueryable(), search, status).ToListAsync(ct);

    private static IQueryable<Customer> ApplyFilters(IQueryable<Customer> query, string? search, CustomerStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.FullName.Contains(search) || c.ContactNumber.Contains(search));

        if (status is { } s)
            query = query.Where(c => c.Status == s);

        return query;
    }

    public Task<List<CustomerId>> SearchIdsByNameAsync(string search, CancellationToken ct = default) =>
        _db.Customers.AsNoTracking().Where(c => c.FullName.Contains(search)).Select(c => c.Id).ToListAsync(ct);

    public async Task<Dictionary<CustomerId, string>> GetNamesByIdsAsync(IReadOnlyCollection<CustomerId> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<CustomerId, string>();

        return await _db.Customers
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
    }

    public void Add(Customer customer) => _db.Customers.Add(customer);
}
