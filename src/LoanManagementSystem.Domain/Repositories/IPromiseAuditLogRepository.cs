using LoanManagementSystem.Domain.Promises;

namespace LoanManagementSystem.Domain.Repositories;

public interface IPromiseAuditLogRepository
{
    Task<List<PromiseAuditLogEntry>> GetByPromiseIdAsync(PromiseToPayId promiseId, CancellationToken ct = default);

    void Add(PromiseAuditLogEntry entry);
}
