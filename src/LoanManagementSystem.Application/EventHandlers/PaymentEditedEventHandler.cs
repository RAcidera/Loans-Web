using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a payment being edited by revising the matching cash_ledger
/// `payment_received` entry and loan_ledger row in place, rather than
/// leaving them stale — see CashLedgerEntry.ReviseForPaymentEdit's doc
/// comment for why this is a deliberate exception to this codebase's
/// otherwise append-only ledger rule. Payments recorded before this
/// feature existed have no SourcePaymentId to match against, so the
/// lookups are allowed to come back empty (nothing to revise, nothing to
/// break) rather than throwing — this handler must not turn an otherwise
/// valid payment edit into a failed request.
/// </summary>
public sealed class PaymentEditedEventHandler : INotificationHandler<PaymentEditedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentEditedEventHandler(ICashLedgerRepository cashLedgerRepository, ILoanLedgerRepository loanLedgerRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PaymentEditedDomainEvent notification, CancellationToken ct)
    {
        var cashEntry = await _cashLedgerRepository.GetBySourcePaymentIdAsync(notification.PaymentId, ct);
        cashEntry?.Revise(notification.NewAmountPaid, notification.NewPaymentDate);

        var loanEntry = await _loanLedgerRepository.GetByPaymentReferenceAsync(notification.LoanId, notification.PaymentId, ct);
        loanEntry?.ReviseForPaymentEdit(notification.NewAmountPaid, notification.ResultingBalance, notification.NewPaymentDate);

        if (cashEntry is not null || loanEntry is not null)
            await _unitOfWork.SaveChangesAsync(ct);
    }
}
