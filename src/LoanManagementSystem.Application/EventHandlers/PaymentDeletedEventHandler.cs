using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a payment being deleted by removing the matching cash_ledger
/// `payment_received` entry and loan_ledger row, rather than leaving them
/// as orphans that reference a payment which no longer exists — see
/// PaymentDeletedDomainEvent's doc comment. Every loan_ledger row created
/// after the removed one has its stamped RunningBalance shifted so the
/// ledger's history stays internally consistent with the loan's real,
/// post-deletion Balance. Payments recorded before SourcePaymentId/
/// ReferenceId tracking existed have no row to find here, so the lookups
/// are allowed to come back empty (nothing to remove, nothing to break)
/// rather than throwing — this handler must not turn an otherwise valid
/// payment deletion into a failed request.
/// </summary>
public sealed class PaymentDeletedEventHandler : INotificationHandler<PaymentDeletedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentDeletedEventHandler(ICashLedgerRepository cashLedgerRepository, ILoanLedgerRepository loanLedgerRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PaymentDeletedDomainEvent notification, CancellationToken ct)
    {
        var changed = false;

        var cashEntry = await _cashLedgerRepository.GetBySourcePaymentIdAsync(notification.PaymentId, ct);
        if (cashEntry is not null)
        {
            _cashLedgerRepository.Remove(cashEntry);
            changed = true;
        }

        var loanEntry = await _loanLedgerRepository.GetByReferenceIdAsync(notification.LoanId, notification.PaymentId.ToString(), ct);
        if (loanEntry is not null)
        {
            var laterEntries = await _loanLedgerRepository.GetAfterAsync(notification.LoanId, loanEntry.CreatedAtUtc, ct);
            var delta = loanEntry.Credit.Amount - loanEntry.Debit.Amount;
            foreach (var later in laterEntries)
                later.ShiftRunningBalance(delta);

            _loanLedgerRepository.Remove(loanEntry);
            changed = true;
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync(ct);
    }
}
