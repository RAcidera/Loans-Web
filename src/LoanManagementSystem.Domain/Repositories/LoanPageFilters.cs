using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

/// <summary>
/// The Loans list page's filter set (spec "Loan Search and Filtering") —
/// bundled into one record rather than more flat parameters on
/// ILoanRepository.GetPageAsync/GetFilteredAsync, which already carry
/// paging/sort/search parameters of their own.
/// </summary>
public sealed record LoanPageFilters(
    LoanStatus? Status = null,
    LoanClassification? Classification = null,
    DateOnly? LoanDateFrom = null,
    DateOnly? LoanDateTo = null,
    DateOnly? DueDateFrom = null,
    DateOnly? DueDateTo = null,
    bool BadLoansOnly = false,
    bool OverdueOnly = false);
