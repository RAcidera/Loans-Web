using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Commands.UpdatePromise;

public sealed record UpdatePromiseCommand(string PromiseId, string PromiseDate, decimal Amount, string Notes, string ModifiedBy) : IRequest<PromiseToPayDto>;

public sealed class UpdatePromiseCommandHandler : IRequestHandler<UpdatePromiseCommand, PromiseToPayDto>
{
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePromiseCommandHandler(
        IPromiseToPayRepository promiseRepository, ICustomerRepository customerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _promiseRepository = promiseRepository;
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PromiseToPayDto> Handle(UpdatePromiseCommand request, CancellationToken ct)
    {
        var id = PromiseToPayId.Parse(request.PromiseId);
        var promise = await _promiseRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(PromiseToPay), request.PromiseId);

        promise.Update(DateOnly.Parse(request.PromiseDate), Money.Of(request.Amount), request.Notes, request.ModifiedBy);
        await _unitOfWork.SaveChangesAsync(ct); // also flushes PromiseUpdatedDomainEvent → promise_audit_log

        var customer = await _customerRepository.GetByIdAsync(promise.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), promise.CustomerId.ToString());
        var loan = await _loanRepository.GetByIdAsync(promise.LoanId, ct)
            ?? throw new NotFoundException(nameof(Loan), promise.LoanId.ToString());

        return promise.ToDto(customer.FullName, loan.LoanNumber);
    }
}
