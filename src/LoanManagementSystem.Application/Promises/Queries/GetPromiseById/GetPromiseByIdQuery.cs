using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Queries.GetPromiseById;

public sealed record GetPromiseByIdQuery(string PromiseId) : IRequest<PromiseToPayDto>;

public sealed class GetPromiseByIdQueryHandler : IRequestHandler<GetPromiseByIdQuery, PromiseToPayDto>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;

    public GetPromiseByIdQueryHandler(IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
    }

    public async Task<PromiseToPayDto> Handle(GetPromiseByIdQuery request, CancellationToken ct)
    {
        var id = PromiseToPayId.Parse(request.PromiseId);
        var promise = await _promiseRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PromiseToPay), request.PromiseId);

        var customer = await _customerRepository.GetByIdAsync(promise.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), promise.CustomerId.ToString());
        var loan = await _loanRepository.GetByIdAsync(promise.LoanId, ct)
            ?? throw new NotFoundException(nameof(Loan), promise.LoanId.ToString());

        return promise.ToDto(customer.FullName, loan.LoanNumber);
    }
}
