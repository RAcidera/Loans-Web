using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(LoanId id, CancellationToken ct = default);
    Task<List<Loan>> GetAllAsync(CancellationToken ct = default);
    Task<List<Loan>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default);

    /// <summary>
    /// All loans with Extensions and Payments eagerly loaded — used by
    /// report queries that aggregate across every loan's child history
    /// (e.g. payments collected or extensions granted within a date range),
    /// not just fields that live directly on Loan.
    /// </summary>
    Task<List<Loan>> GetAllWithDetailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Flattened, newest-first payments across all loans, joined with the
    /// owning customer's name — backs the dashboard's recent-payments feed.
    /// Deliberately not "load every Loan and flatten in memory": this is a
    /// read-heavy query the repository can satisfy more directly.
    /// </summary>
    Task<List<(Payment Payment, string CustomerName, int LoanNumber)>> GetRecentPaymentsAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Loan counts per customer, scoped to just the given customer IDs —
    /// used by the Customers list page's server-side paging to avoid
    /// loading every loan just to compute a per-row count.
    /// </summary>
    Task<Dictionary<CustomerId, int>> GetLoanCountsByCustomerAsync(
        IReadOnlyCollection<CustomerId> customerIds, CancellationToken ct = default);

    /// <summary>
    /// One page of loans, plus the total count of matching rows — backs
    /// the Loans list page's server-side paging. <paramref name="search"/>
    /// matches loan number (digits) or loan status directly, and customer
    /// name indirectly via <paramref name="matchingCustomerIds"/> (resolved
    /// by the caller through ICustomerRepository.SearchIdsByNameAsync,
    /// since Loan and Customer are separate aggregates with no EF
    /// navigation property between them). <paramref name="sortBy"/> is
    /// restricted to loan-native columns (loanNumber, principalAmount,
    /// dueDate, balance, status) for the same reason — sorting by customer
    /// name would need the same cross-aggregate join. <paramref name="filters"/>
    /// is the spec's "Loan Search and Filtering" set; <paramref name="asOfDate"/>
    /// lets an Overdue filter/status match loans whose stored Status column
    /// hasn't been refreshed yet (RefreshOverdueStatus is read-time-only and
    /// never persisted — see the README's documented gap) by comparing
    /// DueDate directly instead of trusting the stored column alone.
    /// </summary>
    Task<(List<Loan> Items, int TotalCount)> GetPageAsync(
        int pageIndex, int pageSize, string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        string? sortBy, string? sortDir, LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default);

    /// <summary>
    /// Every loan matching the same filters as GetPageAsync, with no
    /// paging — backs the Loans list page's footer totals (spec "Loan Grid
    /// Footer Totals"), which sum across the whole filtered result set, not
    /// just the visible page.
    /// </summary>
    Task<List<Loan>> GetFilteredAsync(
        string? search, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        LoanPageFilters filters, DateOnly asOfDate, CancellationToken ct = default);

    /// <summary>
    /// Tracked + Documents included — the path UploadDocument/DeleteDocument
    /// commands use. Deliberately separate from GetByIdAsync (which already
    /// Includes Extensions/Payments) so those two commonly-used paths don't
    /// also pay for downloading every document's VARBINARY(MAX) content.
    /// </summary>
    Task<Loan?> GetByIdWithDocumentsAsync(LoanId id, CancellationToken ct = default);

    /// <summary>
    /// Document metadata only (no byte content) — backs the Loan Details
    /// "Documents" tab's list view. A tuple projection, not
    /// List&lt;LoanDocument&gt;, for the same reason as
    /// ICustomerRepository.GetDocumentsMetadataAsync.
    /// </summary>
    Task<List<(LoanDocumentId Id, string OriginalFileName, string ContentType, long FileSizeBytes, DateTime UploadedAtUtc, string UploadedBy)>> GetDocumentsMetadataAsync(
        LoanId loanId, CancellationToken ct = default);

    /// <summary>One document's full content — backs the download endpoint. Null if the id doesn't exist on this loan.</summary>
    Task<LoanDocument?> GetDocumentContentAsync(LoanId loanId, LoanDocumentId documentId, CancellationToken ct = default);

    void Add(Loan loan);
}
