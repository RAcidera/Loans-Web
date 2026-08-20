using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetPaymentsTotals;

/// <summary>Same filter set as GetPaymentsPageQuery, minus paging/sort — backs the Payments list's footer total.</summary>
public sealed record GetPaymentsTotalsQuery(
    string? LoanSearch, string? CustomerSearch, DateOnly? DateFrom, DateOnly? DateTo,
    DateOnly? CreatedAtFrom = null, DateOnly? CreatedAtTo = null)
    : IRequest<PaymentsTotalsDto>;

public sealed class GetPaymentsTotalsQueryHandler : IRequestHandler<GetPaymentsTotalsQuery, PaymentsTotalsDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetPaymentsTotalsQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<PaymentsTotalsDto> Handle(GetPaymentsTotalsQuery request, CancellationToken ct)
    {
        var effectiveCustomerIds = await ResolveCustomerIdsAsync(_customerRepository, request.CustomerSearch, ct);

        var rows = await _loanRepository.GetPaymentsFilteredAsync(
            request.LoanSearch, effectiveCustomerIds, request.DateFrom, request.DateTo, ct,
            request.CreatedAtFrom, request.CreatedAtTo);

        return new PaymentsTotalsDto(
            TotalAmountPaid: rows.Sum(r => r.Payment.AmountPaid.Amount),
            Count: rows.Count);
    }

    // Shared with ExportPaymentsXlsxQuery so the two filter customer-name
    // search identically — see GetPaymentsPageQuery's doc comment for why
    // an empty-match search needs a sentinel id rather than being treated
    // as "no filter".
    internal static async Task<List<CustomerId>> ResolveCustomerIdsAsync(ICustomerRepository customerRepository, string? customerSearch, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerSearch))
            return new List<CustomerId>();

        var matches = await customerRepository.SearchIdsByNameAsync(customerSearch, ct);
        return matches.Count == 0 ? new List<CustomerId> { CustomerId.New() } : matches;
    }
}
