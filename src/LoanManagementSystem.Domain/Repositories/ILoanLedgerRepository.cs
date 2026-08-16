using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ILoanLedgerRepository
{
    /// <summary>Chronological history for one loan — backs the Loan Details ledger view and the Payments/Extensions tabs' Running Balance columns.</summary>
    Task<List<LoanLedgerEntry>> GetByLoanIdAsync(LoanId loanId, CancellationToken ct = default);

    /// <summary>Whether this loan already has any ledger entries — used by the startup backfill to skip loans that already went through the normal event-driven path.</summary>
    Task<bool> AnyForLoanAsync(LoanId loanId, CancellationToken ct = default);

    /// <summary>Finds this loan's Payment-type row by ReferenceId (the PaymentId, already unique) — used by PaymentEditedEventHandler to revise it in place.</summary>
    Task<LoanLedgerEntry?> GetByPaymentReferenceAsync(LoanId loanId, PaymentId paymentId, CancellationToken ct = default);

    /// <summary>Finds this loan's row by a plain ReferenceId string (a PaymentId or LoanExtensionId's ToString()) — used by PaymentDeletedEventHandler/LoanExtensionDeletedEventHandler to find the row to remove.</summary>
    Task<LoanLedgerEntry?> GetByReferenceIdAsync(LoanId loanId, string referenceId, CancellationToken ct = default);

    /// <summary>Every row for this loan strictly after the given one, in chronological (CreatedAtUtc) order — used to shift stamped RunningBalance values back into agreement with the loan's real Balance after an earlier row is removed.</summary>
    Task<List<LoanLedgerEntry>> GetAfterAsync(LoanId loanId, DateTime createdAtUtc, CancellationToken ct = default);

    void Add(LoanLedgerEntry entry);

    void Remove(LoanLedgerEntry entry);
}
