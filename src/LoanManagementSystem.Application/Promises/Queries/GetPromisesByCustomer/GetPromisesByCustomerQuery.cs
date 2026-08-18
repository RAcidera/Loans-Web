using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Queries.GetPromisesByCustomer;

/// <summary>Backs the Customer Profile "Promises" tab (requirements §23's promise-detail surfaced from the customer record, per the implementation plan's Phase 3 assumption — no standalone Promises list page).</summary>
public sealed record GetPromisesByCustomerQuery(string CustomerId) : IRequest<List<PromiseToPayDto>>;

public sealed class GetPromisesByCustomerQueryHandler : IRequestHandler<GetPromisesByCustomerQuery, List<PromiseToPayDto>>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetPromisesByCustomerQueryHandler(IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<List<PromiseToPayDto>> Handle(GetPromisesByCustomerQuery request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var promises = await _promiseRepository.GetByCustomerAsync(customerId, ct);
        if (promises.Count == 0) return new List<PromiseToPayDto>();

        var customer = await _customerRepository.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        // No batch lookup for loan numbers by id — same trade-off as
        // SearchDiaryEntriesQueryHandler's loan resolution: a customer's
        // promise list is expected to be small.
        var loanNumbers = new Dictionary<LoanId, int>();
        foreach (var loanId in promises.Select(p => p.LoanId).Distinct())
        {
            var loan = await _loanRepository.GetByIdAsync(loanId, ct);
            if (loan is not null) loanNumbers[loanId] = loan.LoanNumber;
        }

        return promises.Select(p => p.ToDto(customer.FullName, loanNumbers.GetValueOrDefault(p.LoanId))).ToList();
    }
}
