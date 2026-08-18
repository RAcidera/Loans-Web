using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.ValueObjects;

namespace LoanManagementSystem.Domain.Promises;

/// <summary>
/// Promise-to-Pay aggregate root (requirements §20) — an irregular-payment
/// commitment tied to a specific customer/loan, tracked through
/// Pending/Rescheduled (still active) to a terminal Kept/Missed/Cancelled
/// state. Its own aggregate, separate from Loan, the same way CashLedgerEntry
/// and DiaryEntry are — a promise is a fact about an expectation, not a
/// mutation of the loan itself.
/// </summary>
public class PromiseToPay : AggregateRoot<PromiseToPayId>
{
    public CustomerId CustomerId { get; private set; }
    public LoanId LoanId { get; private set; }
    public DateOnly PromiseDate { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Notes { get; private set; } = string.Empty;
    public PromiseStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string ModifiedBy { get; private set; } = string.Empty;
    public DateTime ModifiedAtUtc { get; private set; }

    private PromiseToPay() { } // EF Core

    private PromiseToPay(PromiseToPayId id, CustomerId customerId, LoanId loanId, DateOnly promiseDate, Money amount, string notes, string createdBy)
        : base(id)
    {
        CustomerId = customerId;
        LoanId = loanId;
        PromiseDate = promiseDate;
        Amount = amount;
        Notes = notes;
        Status = PromiseStatus.Pending;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedBy = createdBy;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public static PromiseToPay Create(CustomerId customerId, LoanId loanId, DateOnly promiseDate, Money amount, string notes, string createdBy)
    {
        if (amount.Amount <= 0)
            throw new DomainException("A promise-to-pay amount must be greater than zero.");

        var promise = new PromiseToPay(PromiseToPayId.New(), customerId, loanId, promiseDate, amount, notes?.Trim() ?? string.Empty, createdBy);
        promise.RaiseDomainEvent(new PromiseCreatedDomainEvent(promise.Id, createdBy));
        return promise;
    }

    /// <summary>Edits the promise's date/amount/notes without changing its Status — only valid while the promise is still active (Pending/Rescheduled); a Kept/Missed/Cancelled promise is history, not something to correct in place.</summary>
    public void Update(DateOnly promiseDate, Money amount, string notes, string modifiedBy)
    {
        EnsureActionable();
        if (amount.Amount <= 0)
            throw new DomainException("A promise-to-pay amount must be greater than zero.");

        PromiseDate = promiseDate;
        Amount = amount;
        Notes = notes?.Trim() ?? string.Empty;
        ModifiedBy = modifiedBy;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PromiseUpdatedDomainEvent(Id, modifiedBy));
    }

    public void MarkKept(string performedBy)
    {
        EnsureActionable();
        Status = PromiseStatus.Kept;
        ModifiedBy = performedBy;
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PromiseKeptDomainEvent(Id, performedBy));
    }

    public void MarkMissed(string performedBy)
    {
        EnsureActionable();
        Status = PromiseStatus.Missed;
        ModifiedBy = performedBy;
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PromiseMissedDomainEvent(Id, performedBy));
    }

    /// <summary>Moves the promise to a new date rather than creating a new record — customer payments are irregular enough (requirements §20) that a promise slipping is expected, not exceptional. Can be rescheduled more than once.</summary>
    public void Reschedule(DateOnly newPromiseDate, string performedBy)
    {
        EnsureActionable();
        PromiseDate = newPromiseDate;
        Status = PromiseStatus.Rescheduled;
        ModifiedBy = performedBy;
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PromiseRescheduledDomainEvent(Id, newPromiseDate, performedBy));
    }

    public void Cancel(string performedBy)
    {
        EnsureActionable();
        Status = PromiseStatus.Cancelled;
        ModifiedBy = performedBy;
        ModifiedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PromiseCancelledDomainEvent(Id, performedBy));
    }

    private void EnsureActionable()
    {
        if (Status is PromiseStatus.Kept or PromiseStatus.Missed or PromiseStatus.Cancelled)
            throw new DomainException($"This promise is already {Status.ToString().ToLowerInvariant()} and cannot be changed further.");
    }
}
