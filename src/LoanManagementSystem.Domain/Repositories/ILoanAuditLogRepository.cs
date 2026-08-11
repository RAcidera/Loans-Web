using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ILoanAuditLogRepository
{
    /// <summary>Chronological history for one loan — backs the Loan Details "Audit Log" tab.</summary>
    Task<List<LoanAuditLogEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default);

    void Add(LoanAuditLogEntry entry);
}
