using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

/// <summary>
/// The Loans list page's filter set (spec "Loan Search and Filtering") —
/// bundled into one record rather than more flat parameters on
/// ILoanRepository.GetPageAsync/GetFilteredAsync, which already carry
/// paging/sort/search parameters of their own.
/// </summary>
/// <summary>Status/Classification are collections — the Loans list allows selecting multiple of each (e.g. Active + Extended) rather than one at a time; an empty/null collection means "no filter on this field".</summary>
public sealed record LoanPageFilters(
    IReadOnlyCollection<LoanStatus>? Statuses = null,
    IReadOnlyCollection<LoanClassification>? Classifications = null,
    DateOnly? LoanDateFrom = null,
    DateOnly? LoanDateTo = null,
    DateOnly? DueDateFrom = null,
    DateOnly? DueDateTo = null,
    bool BadLoansOnly = false,
    bool OverdueOnly = false);
