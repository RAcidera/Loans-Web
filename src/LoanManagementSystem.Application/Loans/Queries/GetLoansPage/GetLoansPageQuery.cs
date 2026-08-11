using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoansPage;

/// <summary>
/// Status/Classification are the wire-string form (e.g. "overdue",
/// "badloan") matching LoanDto's own casing — parsed to their enums in the
/// handler, same as ChangeLoanClassificationCommand does for a single
/// value. The five filters after SortDir are spec's "Loan Search and
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

    public GetLoansPageQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<LoanDto>> Handle(GetLoansPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
        LoanStatus? status = Enum.TryParse<LoanStatus>(request.Status, ignoreCase: true, out var parsedStatus) ? parsedStatus : null;
        LoanClassification? classification = Enum.TryParse<LoanClassification>(request.Classification, ignoreCase: true, out var parsedClassification) ? parsedClassification : null;

        return new LoanPageFilters(
            Status: status,
            Classification: classification,
            LoanDateFrom: request.LoanDateFrom,
            LoanDateTo: request.LoanDateTo,
            DueDateFrom: request.DueDateFrom,
            DueDateTo: request.DueDateTo,
            BadLoansOnly: request.BadLoansOnly,
            OverdueOnly: request.OverdueOnly);
    }
}
