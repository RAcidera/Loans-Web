using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.SearchDiaryEntries;

/// <summary>Requirements §12's filter set. Search matches Title/Notes/Tags directly (IDiaryRepository.SearchAsync), plus Customer Name/Code and Loan Number (requirements §4) — the handler below resolves those into id lists first, the same two-pass approach GetPaymentsPageAsync's loanSearch/customerSearch already uses elsewhere in this codebase, since Diary/Customer/Loan are separate aggregates with no EF navigation property to join through directly.</summary>
public sealed record SearchDiaryEntriesQuery(
    string? SearchText = null,
    string? CategoryId = null,
    string? DateFrom = null,
    string? DateTo = null,
    string? CustomerId = null,
    string? LoanId = null,
    bool? HasFinancialSnapshot = null,
    bool? HasReminder = null
) : IRequest<List<DiaryEntryDto>>;

/// <summary>Backs the Diary timeline (requirements §11) — sorted EntryDateTime DESC by IDiaryRepository.SearchAsync, not paged (a chronological timeline, not a data grid).</summary>
public sealed class SearchDiaryEntriesQueryHandler : IRequestHandler<SearchDiaryEntriesQuery, List<DiaryEntryDto>>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _categoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public SearchDiaryEntriesQueryHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository categoryRepository,
        ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _diaryRepository = diaryRepository;
        _categoryRepository = categoryRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<List<DiaryEntryDto>> Handle(SearchDiaryEntriesQuery request, CancellationToken ct)
    {
        List<CustomerId>? matchingCustomerIds = null;
        List<LoanId>? matchingLoanIds = null;
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var text = request.SearchText;
            var allCustomers = await _customerRepository.GetAllAsync(ct);
            matchingCustomerIds = allCustomers
                .Where(c => c.FullName.Contains(text, StringComparison.OrdinalIgnoreCase) || MappingExtensions.FormatCustomerCode(c.CustomerNumber).Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToList();

            var allLoans = await _loanRepository.GetAllAsync(ct);
            matchingLoanIds = allLoans
                .Where(l => MappingExtensions.FormatLoanNumber(l.LoanNumber).Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Id)
                .ToList();
        }

        var filters = new DiarySearchFilters(
            SearchText: request.SearchText,
            CategoryId: request.CategoryId is not null ? DiaryCategoryId.Parse(request.CategoryId) : null,
            DateFrom: request.DateFrom is not null ? DateOnly.Parse(request.DateFrom) : null,
            DateTo: request.DateTo is not null ? DateOnly.Parse(request.DateTo) : null,
            CustomerId: request.CustomerId is not null ? CustomerId.Parse(request.CustomerId) : null,
            LoanId: request.LoanId is not null ? LoanId.Parse(request.LoanId) : null,
            HasFinancialSnapshot: request.HasFinancialSnapshot,
            HasReminder: request.HasReminder,
            MatchingCustomerIds: matchingCustomerIds,
            MatchingLoanIds: matchingLoanIds);

        var entries = await _diaryRepository.SearchAsync(filters, ct);
        if (entries.Count == 0)
            return new List<DiaryEntryDto>();

        var categoriesById = (await _categoryRepository.GetAllAsync(ct)).ToDictionary(c => c.Id);

        var customerIds = entries.Where(e => e.CustomerId is not null).Select(e => e.CustomerId!.Value).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        // No batch lookup exists for loan numbers by id (unlike customer
        // names) — diary entries with a linked loan are expected to be a
        // small minority of the result set, so N individual lookups here is
        // an acceptable trade-off over adding a new ILoanRepository method
        // for a rarely-hit path.
        var loanNumbers = new Dictionary<LoanId, int>();
        foreach (var loanId in entries.Where(e => e.LoanId is not null).Select(e => e.LoanId!.Value).Distinct())
        {
            var loan = await _loanRepository.GetByIdAsync(loanId, ct);
            if (loan is not null) loanNumbers[loanId] = loan.LoanNumber;
        }

        return entries
            .Where(e => categoriesById.ContainsKey(e.CategoryId))
            .Select(e => e.ToDto(
                categoriesById[e.CategoryId],
                e.CustomerId is { } cid ? customerNames.GetValueOrDefault(cid) : null,
                e.LoanId is { } lid && loanNumbers.TryGetValue(lid, out var number) ? number : null))
            .ToList();
    }
}
