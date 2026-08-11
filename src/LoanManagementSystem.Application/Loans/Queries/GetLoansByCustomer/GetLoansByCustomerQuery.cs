using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoansByCustomer;

public sealed record GetLoansByCustomerQuery(string CustomerId) : IRequest<List<LoanDto>>;

public sealed class GetLoansByCustomerQueryHandler : IRequestHandler<GetLoansByCustomerQuery, List<LoanDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetLoansByCustomerQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<List<LoanDto>> Handle(GetLoansByCustomerQuery request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        var loans = await _loanRepository.GetByCustomerAsync(customerId, ct);

        return loans.Select(l => l.ToDto(customer?.FullName ?? "Unknown")).ToList();
    }
}
