using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;

namespace LoanManagementSystem.Domain.Repositories;

public interface IPromiseToPayRepository
{
    Task<PromiseToPay?> GetByIdAsync(PromiseToPayId id, CancellationToken ct = default);

    /// <summary>Every promise for one customer, newest PromiseDate first — backs the Customer Profile "Promises" tab.</summary>
    Task<List<PromiseToPay>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default);

    /// <summary>Every promise for one loan, newest PromiseDate first — backs the Loan Details "Promises" tab.</summary>
    Task<List<PromiseToPay>> GetByLoanAsync(LoanId loanId, CancellationToken ct = default);

    /// <summary>Every promise whose PromiseDate falls within [from, to], regardless of status — backs the Calendar's "Promise to Pay" event source (requirements §19).</summary>
    Task<List<PromiseToPay>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    void Add(PromiseToPay promise);

    void Remove(PromiseToPay promise);
}
