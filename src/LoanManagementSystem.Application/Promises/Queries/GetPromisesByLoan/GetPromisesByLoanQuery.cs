using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Queries.GetPromisesByLoan;

/// <summary>Backs the Loan Details "Promises" tab.</summary>
public sealed record GetPromisesByLoanQuery(string LoanId) : IRequest<List<PromiseToPayDto>>;

public sealed class GetPromisesByLoanQueryHandler : IRequestHandler<GetPromisesByLoanQuery, List<PromiseToPayDto>>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetPromisesByLoanQueryHandler(IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<List<PromiseToPayDto>> Handle(GetPromisesByLoanQuery request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var promises = await _promiseRepository.GetByLoanAsync(loanId, ct);
        if (promises.Count == 0) return new List<PromiseToPayDto>();

        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);
        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);

        return promises.Select(p => p.ToDto(customer?.FullName ?? "Unknown", loan.LoanNumber)).ToList();
    }
}
