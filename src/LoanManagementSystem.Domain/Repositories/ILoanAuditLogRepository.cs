using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ILoanAuditLogRepository
{
    /// <summary>Chronological history for one loan — backs the Loan Details "Audit Log" tab.</summary>
    Task<List<LoanAuditLogEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default);

    /// <summary>Every entry of one action type across all loans — e.g. WrittenOff, whose OccurredAtUtc is the only record of when a loan left Gross Receivables (Monthly Capital Growth).</summary>
    Task<List<LoanAuditLogEntry>> GetByActionAsync(LoanAuditAction action, CancellationToken ct = default);

    void Add(LoanAuditLogEntry entry);
}
