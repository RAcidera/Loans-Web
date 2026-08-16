using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Application.Loans.Queries.GetPaymentsTotals;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetPaymentsPage;

/// <summary>
/// The standalone Payments list page's server-side paging — every payment
/// across every loan, joined with its loan number and customer name.
/// LoanSearch matches the loan number directly (digits only, same
/// convention as GetLoansPageQuery's search); CustomerSearch is resolved
/// into a customer-id list first since Payment/Loan and Customer are
/// separate aggregates with no EF navigation property (same reasoning as
/// GetLoansPageQuery's own customer-name search).
/// </summary>
public sealed record GetPaymentsPageQuery(
    int PageIndex, int PageSize, string? LoanSearch, string? CustomerSearch,
    DateOnly? DateFrom, DateOnly? DateTo, string? SortBy, string? SortDir)
    : IRequest<PagedResult<PaymentWithCustomerDto>>;

public sealed class GetPaymentsPageQueryHandler : IRequestHandler<GetPaymentsPageQuery, PagedResult<PaymentWithCustomerDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetPaymentsPageQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<PaymentWithCustomerDto>> Handle(GetPaymentsPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);

        var effectiveCustomerIds = await GetPaymentsTotalsQueryHandler.ResolveCustomerIdsAsync(_customerRepository, request.CustomerSearch, ct);

        var (rows, totalCount) = await _loanRepository.GetPaymentsPageAsync(
            pageIndex, pageSize, request.LoanSearch, effectiveCustomerIds, request.DateFrom, request.DateTo, request.SortBy, request.SortDir, ct);

        var items = rows.Select(r => r.Payment.ToDtoWithCustomer(r.CustomerId.ToString(), r.CustomerName, r.LoanNumber)).ToList();
        return new PagedResult<PaymentWithCustomerDto>(items, totalCount);
    }
}
