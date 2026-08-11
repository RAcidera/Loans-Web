using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ILoanLedgerRepository
{
    /// <summary>Chronological history for one loan — backs the Loan Details ledger view and the Payments/Extensions tabs' Running Balance columns.</summary>
    Task<List<LoanLedgerEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default);

    /// <summary>Whether this loan already has any ledger entries — used by the startup backfill to skip loans that already went through the normal event-driven path.</summary>
    Task<bool> AnyForLoanAsync(LoanId loanId, CancellationToken ct = default);

    void Add(LoanLedgerEntry entry);
}
