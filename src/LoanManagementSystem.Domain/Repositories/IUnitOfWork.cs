namespace LoanManagementSystem.Domain.Repositories;

/// <summary>
/// One commit point for a use case that may touch more than one
/// repository (e.g. RecordPaymentCommandHandler saves the Loan; the
/// resulting PaymentRecordedDomainEvent handler adds a CashLedgerEntry in
/// the same unit of work) — both go into the same database transaction
/// when SaveChangesAsync is called once at the end of the handler.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
