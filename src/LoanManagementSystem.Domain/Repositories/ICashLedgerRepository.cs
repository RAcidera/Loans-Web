using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Repositories;

public interface ICashLedgerRepository
{
    Task<List<CashLedgerEntry>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Finds the `payment_received` entry mirroring a specific Payment — used by PaymentEditedEventHandler to revise it in place. Null if the payment predates SourcePaymentId being tracked.</summary>
    Task<CashLedgerEntry?> GetBySourcePaymentIdAsync(PaymentId paymentId, CancellationToken ct = default);

    /// <summary>Finds the one `loan_release` entry for a loan (ReferenceId == loan number, unique per loan) — used by LoanOriginationEditedEventHandler to revise it in place.</summary>
    Task<CashLedgerEntry?> GetLoanReleaseEntryAsync(string loanNumber, CancellationToken ct = default);

    /// <summary>Tracked — backs EditCashTransactionCommand/DeleteCashTransactionCommand, which mutate/remove the entry and rely on SaveChanges to persist it.</summary>
    Task<CashLedgerEntry?> GetByIdAsync(CashLedgerEntryId id, CancellationToken ct = default);

    void Add(CashLedgerEntry entry);

    void Remove(CashLedgerEntry entry);
}
