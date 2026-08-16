using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Interest;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;

namespace LoanManagementSystem.Application.Reports;

/// <summary>
/// Shared "load, filter, calculate" pipeline for the Interest Earned
/// Report, used identically by the paged grid, the summary/totals
/// overview, the monthly chart, and both exports — so every one of them
/// runs the exact same calculation as the on-screen report (spec §26's
/// "the PDF must use the same calculations as the screen report").
/// Loans are loaded and filtered once per request; the date-scoped
/// row-building step is split out separately so the monthly chart can
/// reuse the same filtered loan set across 24 different [from,to] windows
/// without re-querying the database each time.
/// </summary>
public sealed class InterestEarnedReportDataProvider
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInterestCalculationService _interestCalculationService;

    public InterestEarnedReportDataProvider(
        ILoanRepository loanRepository, ICustomerRepository customerRepository, IInterestCalculationService interestCalculationService)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _interestCalculationService = interestCalculationService;
    }

    /// <summary>Loads every loan matching the non-date filters (search/status/classification) — the date-range overlap is applied later, per report window, in BuildRows.</summary>
    public async Task<(List<Loan> Loans, Dictionary<CustomerId, string> CustomerNames)> LoadFilteredLoansAsync(
        string? search, string? status, string? classification, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);

        var matchingCustomerIds = string.IsNullOrWhiteSpace(search)
            ? new List<CustomerId>()
            : await _customerRepository.SearchIdsByNameAsync(search, ct);

        IEnumerable<Loan> filtered = loans;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(l =>
                MappingExtensions.FormatLoanNumber(l.LoanNumber).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                matchingCustomerIds.Contains(l.CustomerId));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LoanStatus>(status, ignoreCase: true, out var statusValue))
            filtered = filtered.Where(l => l.Status == statusValue);

        if (!string.IsNullOrWhiteSpace(classification) && Enum.TryParse<LoanClassification>(classification, ignoreCase: true, out var classificationValue))
            filtered = filtered.Where(l => l.Classification == classificationValue);

        var filteredList = filtered.ToList();
        var customerIds = filteredList.Select(l => l.CustomerId).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        return (filteredList, customerNames);
    }

    /// <summary>Builds one row per loan whose active life overlaps [fromDate, toDate] — a loan that hadn't started yet or was already fully settled before the window has nothing to report for that period.</summary>
    public List<InterestEarnedRowDto> BuildRows(
        List<Loan> loans, Dictionary<CustomerId, string> customerNames, DateOnly fromDate, DateOnly toDate, string? interestType)
    {
        var normalizedType = string.IsNullOrWhiteSpace(interestType) ? "all" : interestType.Trim().ToLowerInvariant();

        return loans
            .Where(l => l.StartDate <= toDate && l.DueDate >= fromDate)
            .Select(loan => BuildRow(loan, fromDate, toDate, normalizedType, customerNames))
            .ToList();
    }

    private InterestEarnedRowDto BuildRow(
        Loan loan, DateOnly fromDate, DateOnly toDate, string interestType, Dictionary<CustomerId, string> customerNames)
    {
        var breakdown = _interestCalculationService.Calculate(loan, fromDate, toDate);

        var includeOriginal = interestType != "extension";
        var includeExtension = interestType != "original";

        var earnedBefore = (includeOriginal ? breakdown.OriginalEarnedBeforePeriod : 0m) + (includeExtension ? breakdown.ExtensionEarnedBeforePeriod : 0m);
        var earnedThisPeriod = (includeOriginal ? breakdown.OriginalEarnedThisPeriod : 0m) + (includeExtension ? breakdown.ExtensionEarnedThisPeriod : 0m);
        var totalEarned = (includeOriginal ? breakdown.OriginalTotalEarned : 0m) + (includeExtension ? breakdown.ExtensionTotalEarned : 0m);
        // Adjustment only has meaning against the original contract interest — extensions have no rate-based "original" to adjust against.
        var adjustment = includeOriginal ? breakdown.InterestAdjustment : 0m;
        // FinalEarned = TotalEarned, not TotalEarned + adjustment: InterestCalculationService already caps
        // accrual at the ADJUSTED contract interest (see its class doc comment), so TotalEarned is already
        // the post-adjustment figure. Adding Adjustment again here would double-count it — e.g. a loan whose
        // interest was discounted from 240 to 144 would show 144 + (-96) = 48 instead of the correct 144.
        // Adjustment is kept as its own column purely for audit/transparency (spec §2.4/§7), not as an
        // additional term to sum into the total.
        var finalEarned = totalEarned;

        var customerName = customerNames.TryGetValue(loan.CustomerId, out var name) ? name : "Unknown";

        return new InterestEarnedRowDto(
            LoanId: loan.Id.ToString(),
            LoanNumber: MappingExtensions.FormatLoanNumber(loan.LoanNumber),
            CustomerId: loan.CustomerId.ToString(),
            CustomerName: customerName,
            LoanDate: loan.StartDate.ToString("yyyy-MM-dd"),
            DueDate: loan.DueDate.ToString("yyyy-MM-dd"),
            Principal: loan.PrincipalAmount.Amount,
            ContractInterest: breakdown.OriginalContractInterest,
            ExtensionInterest: breakdown.ExtensionContractInterest,
            EarnedBeforePeriod: earnedBefore,
            EarnedThisPeriod: earnedThisPeriod,
            TotalEarned: totalEarned,
            Adjustment: adjustment,
            FinalEarned: finalEarned,
            Status: loan.Status.ToString().ToLowerInvariant(),
            Classification: loan.Classification.ToString().ToLowerInvariant(),
            OriginalEarnedThisPeriod: includeOriginal ? breakdown.OriginalEarnedThisPeriod : 0m,
            ExtensionEarnedThisPeriod: includeExtension ? breakdown.ExtensionEarnedThisPeriod : 0m
        );
    }
}
