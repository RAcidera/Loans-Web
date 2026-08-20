using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoansPage;

/// <summary>
/// Status/Classification are comma-separated wire-string values (e.g.
/// "active,extended") — the Loans list allows selecting more than one of
/// each at once, matching LoanDto's own casing, parsed to their enums in
/// the handler. The five filters after SortDir are spec's "Loan Search and
/// Filtering" set (3.2).
/// </summary>
public sealed record GetLoansPageQuery(
    int PageIndex, int PageSize, string? Search, string? SortBy, string? SortDir,
    string? Status = null, string? Classification = null,
    DateOnly? LoanDateFrom = null, DateOnly? LoanDateTo = null,
    DateOnly? DueDateFrom = null, DateOnly? DueDateTo = null,
    bool BadLoansOnly = false, bool OverdueOnly = false)
    : IRequest<PagedResult<LoanDto>>;

public sealed class GetLoansPageQueryHandler : IRequestHandler<GetLoansPageQuery, PagedResult<LoanDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetLoansPageQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _appDateTime = appDateTime;
    }

    public async Task<PagedResult<LoanDto>> Handle(GetLoansPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);
        var today = _appDateTime.Today;

        // Loan and Customer are separate aggregates with no EF navigation
        // property between them — resolve a customer-name search into an
        // ID list first, rather than joining across repositories.
        var matchingCustomerIds = string.IsNullOrWhiteSpace(request.Search)
            ? new List<Domain.Customers.CustomerId>()
            : await _customerRepository.SearchIdsByNameAsync(request.Search, ct);

        var filters = BuildFilters(request);

        var (loans, totalCount) = await _loanRepository.GetPageAsync(
            pageIndex, pageSize, request.Search, matchingCustomerIds, request.SortBy, request.SortDir, filters, today, ct);

        var customerIds = loans.Select(l => l.CustomerId).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        var items = loans
            .Select(loan =>
            {
                loan.RefreshOverdueStatus(today);
                var customerName = customerNames.TryGetValue(loan.CustomerId, out var name) ? name : "Unknown";
                return loan.ToDto(customerName);
            })
            .ToList();

        return new PagedResult<LoanDto>(items, totalCount);
    }

    internal static LoanPageFilters BuildFilters(GetLoansPageQuery request)
    {
        return new LoanPageFilters(
            Statuses: ParseEnumList<LoanStatus>(request.Status),
            Classifications: ParseEnumList<LoanClassification>(request.Classification),
            LoanDateFrom: request.LoanDateFrom,
            LoanDateTo: request.LoanDateTo,
            DueDateFrom: request.DueDateFrom,
            DueDateTo: request.DueDateTo,
            BadLoansOnly: request.BadLoansOnly,
            OverdueOnly: request.OverdueOnly);
    }

    private static List<T>? ParseEnumList<T>(string? commaSeparated) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(commaSeparated)) return null;

        var values = commaSeparated
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => Enum.TryParse<T>(token, ignoreCase: true, out var value) ? value : (T?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        return values.Count > 0 ? values : null;
    }
}
