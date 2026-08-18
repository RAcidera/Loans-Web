using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Commands.CreatePromise;

public sealed record CreatePromiseCommand(
    string CustomerId,
    string LoanId,
    string PromiseDate,
    decimal Amount,
    string Notes,
    string CreatedBy
) : IRequest<PromiseToPayDto>;

public sealed class CreatePromiseCommandHandler : IRequestHandler<CreatePromiseCommand, PromiseToPayDto>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePromiseCommandHandler(
        IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PromiseToPayDto> Handle(CreatePromiseCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.Parse(request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var promise = PromiseToPay.Create(customerId, loanId, DateOnly.Parse(request.PromiseDate), Money.Of(request.Amount), request.Notes, request.CreatedBy);

        _promiseRepository.Add(promise);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes PromiseCreatedDomainEvent → promise_audit_log

        return promise.ToDto(customer.FullName, loan.LoanNumber);
    }
}
