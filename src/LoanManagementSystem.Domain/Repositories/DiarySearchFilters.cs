using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

/// <summary>
/// The Diary timeline's filter set (requirements §12) — bundled into one
/// record for the same reason as LoanPageFilters.
/// </summary>
/// <param name="SearchText">Matched directly against Title/Notes/Tags.</param>
/// <param name="MatchingCustomerIds">
/// Resolved by the query handler (not this repository) from SearchText
/// against Customer FullName/CustomerCode — requirements §4's "Search must
/// match ... Customer Name, Customer Code". A diary entry whose CustomerId
/// is in this set matches regardless of whether SearchText itself appears
/// in Title/Notes/Tags. Null (not just empty) means "SearchText wasn't
/// provided, don't apply this OR-branch at all".
/// </param>
/// <param name="MatchingLoanIds">Same idea as MatchingCustomerIds, resolved from SearchText against Loan Number (requirements §4).</param>
public sealed record DiarySearchFilters(
    string? SearchText = null,
    DiaryCategoryId? CategoryId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    CustomerId? CustomerId = null,
    LoanId? LoanId = null,
    bool? HasFinancialSnapshot = null,
    bool? HasReminder = null,
    IReadOnlyCollection<CustomerId>? MatchingCustomerIds = null,
    IReadOnlyCollection<LoanId>? MatchingLoanIds = null);
