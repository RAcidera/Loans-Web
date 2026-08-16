using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetCustomerPaymentsPage;

/// <summary>Server-side paging for the Customer Profile's "Payment History" tab. SortBy is "date"/"loan"/"amount", matching the tab's sortable columns.</summary>
public sealed record GetCustomerPaymentsPageQuery(string CustomerId, int PageIndex, int PageSize, string? SortBy = null, string? SortDir = null) : IRequest<PagedResult<PaymentWithLoanDto>>;

public sealed class GetCustomerPaymentsPageQueryHandler : IRequestHandler<GetCustomerPaymentsPageQuery, PagedResult<PaymentWithLoanDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetCustomerPaymentsPageQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<PagedResult<PaymentWithLoanDto>> Handle(GetCustomerPaymentsPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);

        var (items, totalCount) = await _loanRepository.GetPaymentsByCustomerPageAsync(
            CustomerId.Parse(request.CustomerId), pageIndex, pageSize, request.SortBy, request.SortDir, ct);

        var dtos = items.Select(x => x.Payment.ToDtoWithLoan(x.LoanNumber)).ToList();
        return new PagedResult<PaymentWithLoanDto>(dtos, totalCount);
    }
}
