using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Models;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanPaymentsPage;

/// <summary>Server-side paging for the Loan Details "Payments" tab. SortBy is "date", the tab's only sortable column.</summary>
public sealed record GetLoanPaymentsPageQuery(string LoanId, int PageIndex, int PageSize, string? SortBy = null, string? SortDir = null) : IRequest<PagedResult<PaymentDto>>;

public sealed class GetLoanPaymentsPageQueryHandler : IRequestHandler<GetLoanPaymentsPageQuery, PagedResult<PaymentDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetLoanPaymentsPageQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<PagedResult<PaymentDto>> Handle(GetLoanPaymentsPageQuery request, CancellationToken ct)
    {
        var pageIndex = Math.Max(0, request.PageIndex);
        var pageSize = Math.Max(1, request.PageSize);

        var (items, totalCount) = await _loanRepository.GetPaymentsByLoanPageAsync(
            LoanId.Parse(request.LoanId), pageIndex, pageSize, request.SortBy, request.SortDir, ct);

        var dtos = items.Select(p => p.ToDto()).ToList();
        return new PagedResult<PaymentDto>(dtos, totalCount);
    }
}
