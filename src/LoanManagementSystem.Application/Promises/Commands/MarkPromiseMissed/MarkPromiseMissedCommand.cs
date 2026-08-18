using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Commands.MarkPromiseMissed;

public sealed record MarkPromiseMissedCommand(string PromiseId, string PerformedBy) : IRequest<PromiseToPayDto>;

public sealed class MarkPromiseMissedCommandHandler : IRequestHandler<MarkPromiseMissedCommand, PromiseToPayDto>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkPromiseMissedCommandHandler(
        IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PromiseToPayDto> Handle(MarkPromiseMissedCommand request, CancellationToken ct)
    {
        var id = PromiseToPayId.Parse(request.PromiseId);
        var promise = await _promiseRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PromiseToPay), request.PromiseId);

        promise.MarkMissed(request.PerformedBy);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes PromiseMissedDomainEvent → promise_audit_log

        var customer = await _customerRepository.GetByIdAsync(promise.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), promise.CustomerId.ToString());
        var loan = await _loanRepository.GetByIdAsync(promise.LoanId, ct)
            ?? throw new NotFoundException(nameof(Loan), promise.LoanId.ToString());

        return promise.ToDto(customer.FullName, loan.LoanNumber);
    }
}
