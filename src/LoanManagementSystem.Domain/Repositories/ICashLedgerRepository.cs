using LoanManagementSystem.Domain.CashLedger;

namespace LoanManagementSystem.Domain.Repositories;

public interface ICashLedgerRepository
{
    Task<List<CashLedgerEntry>> GetAllAsync(CancellationToken ct = default);
    void Add(CashLedgerEntry entry);
}
