using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetCustomerSummary;

public sealed record GetCustomerSummaryQuery : IRequest<List<CustomerSummaryDto>>;

/// <summary>SRS 3.5's "summaries per customer": total borrowed, total paid, and loan count, one row per customer.</summary>
public sealed class GetCustomerSummaryQueryHandler : IRequestHandler<GetCustomerSummaryQuery, List<CustomerSummaryDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetCustomerSummaryQueryHandler(ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<List<CustomerSummaryDto>> Handle(GetCustomerSummaryQuery request, CancellationToken ct)
    {
        var customers = await _customerRepository.GetAllAsync(ct);
        var loans = await _loanRepository.GetAllAsync(ct);
        var loansByCustomer = loans.ToLookup(l => l.CustomerId);

        return customers
            .Select(customer =>
            {
                var customerLoans = loansByCustomer[customer.Id];
                return new CustomerSummaryDto(
                    CustomerId: customer.Id.ToString(),
                    CustomerName: customer.FullName,
                    TotalBorrowed: customerLoans.Sum(l => l.PrincipalAmount.Amount),
                    TotalPaid: customerLoans.Sum(l => l.TotalPaid.Amount),
                    LoansCount: customerLoans.Count()
                );
            })
            .ToList();
    }
}
