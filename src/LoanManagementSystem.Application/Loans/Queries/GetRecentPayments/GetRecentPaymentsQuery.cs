using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetRecentPayments;

public sealed record GetRecentPaymentsQuery(int Limit = 6) : IRequest<List<PaymentWithCustomerDto>>;

public sealed class GetRecentPaymentsQueryHandler : IRequestHandler<GetRecentPaymentsQuery, List<PaymentWithCustomerDto>>
{
    private readonly ILoanRepository _loanRepository;

    public GetRecentPaymentsQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<List<PaymentWithCustomerDto>> Handle(GetRecentPaymentsQuery request, CancellationToken ct)
    {
        var recent = await _loanRepository.GetRecentPaymentsAsync(request.Limit, ct);
        return recent.Select(r => r.Payment.ToDtoWithCustomer(r.CustomerId.ToString(), r.CustomerName, r.LoanNumber)).ToList();
    }
}
