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
    /// owning customer's id/name — backs the dashboard's recent-payments feed.
    /// Deliberately not "load every Loan and flatten in memory": this is a
    /// read-heavy query the repository can satisfy more directly.
    /// </summary>
    Task<List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)>> GetRecentPaymentsAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// One page of payments across every loan, joined with the owning
    /// loan's number and customer's id/name — backs the standalone Payments
    /// list page's server-side paging. loanSearch matches the loan number
    /// (digits) directly; customerSearch is resolved by the caller into a
    /// customer-id list first via ICustomerRepository.SearchIdsByNameAsync,
    /// same reasoning as GetPageAsync's search param (Loan/Payment and
    /// Customer are separate aggregates with no EF navigation property).
    /// sortBy is "loan"/"customer"/"date"/"amount", defaulting to
    /// newest-date-first when omitted.
    /// </summary>
    Task<(List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)> Items, int TotalCount)> GetPaymentsPageAsync(
        int pageIndex, int pageSize, string? loanSearch, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        DateOnly? dateFrom, DateOnly? dateTo, string? sortBy, string? sortDir, CancellationToken ct = default,
        DateOnly? createdAtFrom = null, DateOnly? createdAtTo = null);

    /// <summary>
    /// Every payment matching the same filters as GetPaymentsPageAsync, with
    /// no paging — backs the Payments list page's footer total and its
    /// Export button, both of which summarize/export the whole filtered
    /// result set, not just the visible page.
    /// </summary>
    Task<List<(Payment Payment, CustomerId CustomerId, string CustomerName, int LoanNumber)>> GetPaymentsFilteredAsync(
        string? loanSearch, IReadOnlyCollection<CustomerId> matchingCustomerIds,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default,
        DateOnly? createdAtFrom = null, DateOnly? createdAtTo = null);

    /// <summary>
    /// One page of a single customer's payments across all of their loans,
    /// plus the total count — backs the Customer Profile's "Payment
    /// History" tab server-side paging. Payment has no repository of its
    /// own (it's a Loan child entity elsewhere), but is still a
    /// directly-queryable EF entity, same as GetRecentPaymentsAsync.
    /// sortBy is one of "date"/"loan"/"amount" (matching the tab's
    /// sortable columns), defaulting to newest-date-first when omitted.
    /// </summary>
    Task<(List<(Payment Payment, int LoanNumber)> Items, int TotalCount)> GetPaymentsByCustomerPageAsync(
        CustomerId customerId, int pageIndex, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default);

    /// <summary>
    /// One page of a single loan's own payments, plus the total count —
    /// backs the Loan Details "Payments" tab's server-side paging. sortBy
    /// is "date" (the tab's only sortable column), defaulting to
    /// newest-date-first when omitted.
    /// </summary>
    Task<(List<Payment> Items, int TotalCount)> GetPaymentsByLoanPageAsync(
        LoanId loanId, int pageIndex, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default);

    /// <summary>
    /// Loan count and total outstanding balance per customer, scoped to
    /// just the given customer IDs — used by the Customers list page's
    /// server-side paging to avoid loading every loan just to compute a
    /// per-row count/balance.
    /// </summary>
    Task<Dictionary<CustomerId, (int LoanCount, decimal OutstandingBalance)>> GetLoanCountsAndBalanceByCustomerAsync(
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
