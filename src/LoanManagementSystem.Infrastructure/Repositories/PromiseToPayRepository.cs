using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class PromiseToPayRepository : IPromiseToPayRepository
{
    private readonly AppDbContext _db;

    public PromiseToPayRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<PromiseToPay?> GetByIdAsync(PromiseToPayId id, CancellationToken ct = default) =>
        _db.Set<PromiseToPay>().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<PromiseToPay>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default) =>
        _db.Set<PromiseToPay>().AsNoTracking().Where(p => p.CustomerId == customerId).OrderByDescending(p => p.PromiseDate).ToListAsync(ct);

    public Task<List<PromiseToPay>> GetByLoanAsync(LoanId loanId, CancellationToken ct = default) =>
        _db.Set<PromiseToPay>().AsNoTracking().Where(p => p.LoanId == loanId).OrderByDescending(p => p.PromiseDate).ToListAsync(ct);

    public Task<List<PromiseToPay>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.Set<PromiseToPay>().AsNoTracking().Where(p => p.PromiseDate >= from && p.PromiseDate <= to).ToListAsync(ct);

    public void Add(PromiseToPay promise) => _db.Set<PromiseToPay>().Add(promise);

    public void Remove(PromiseToPay promise) => _db.Set<PromiseToPay>().Remove(promise);
}
