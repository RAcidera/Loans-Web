using LoanManagementSystem.Domain.Customers;

namespace LoanManagementSystem.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default);

    /// <summary>
    /// Tracked + Documents included — the path UploadDocument/DeleteDocument
    /// commands use, since mutating that collection needs the aggregate
    /// loaded with it (same reasoning as ILoanRepository.GetByIdAsync's
    /// Include of Extensions/Payments). Deliberately not the default
    /// GetByIdAsync, which every other read path uses — Documents carries
    /// VARBINARY(MAX) content, so including it unconditionally would make
    /// every customer fetch pay for downloading every document's bytes.
    /// </summary>
    Task<Customer?> GetByIdWithDocumentsAsync(CustomerId id, CancellationToken ct = default);

    /// <summary>
    /// Document metadata only (no byte content) — backs the Documents tab's
    /// list view. A tuple projection, not List&lt;CustomerDocument&gt;,
    /// because CustomerDocument's setters are private (only Customer.
    /// UploadDocument() constructs one) — the same reasoning as
    /// ILoanRepository.GetRecentPaymentsAsync's tuple return.
    /// </summary>
    Task<List<(CustomerDocumentId Id, string OriginalFileName, string ContentType, long FileSizeBytes, DateTime UploadedAtUtc, string UploadedBy)>> GetDocumentsMetadataAsync(
        CustomerId customerId, CancellationToken ct = default);

    /// <summary>One document's full content — backs the download endpoint. Null if the id doesn't exist on this customer.</summary>
    Task<CustomerDocument?> GetDocumentContentAsync(CustomerId customerId, CustomerDocumentId documentId, CancellationToken ct = default);

    Task<List<Customer>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// One page of customers (filtered by <paramref name="search"/> against
    /// full name/contact number, if provided), plus the total count of
    /// matching rows — backs the Customers list page's server-side paging.
    /// </summary>
    Task<(List<Customer> Items, int TotalCount)> GetPageAsync(
        int pageIndex, int pageSize, string? search, string? sortBy, string? sortDir, CancellationToken ct = default);

    /// <summary>
    /// Customer IDs whose full name contains <paramref name="search"/>
    /// (case-insensitive) — used by the Loans list page's server-side
    /// search to resolve a customer-name query into an ID list without
    /// loading every customer.
    /// </summary>
    Task<List<CustomerId>> SearchIdsByNameAsync(string search, CancellationToken ct = default);

    /// <summary>
    /// Full names for just the given customer IDs — used by the Loans list
    /// page's server-side paging to fill LoanDto.CustomerName for one page
    /// of loans without loading every customer.
    /// </summary>
    Task<Dictionary<CustomerId, string>> GetNamesByIdsAsync(IReadOnlyCollection<CustomerId> ids, CancellationToken ct = default);

    void Add(Customer customer);
}
