using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan's Principal/StartDate being edited by revising the
/// matching cash_ledger `loan_release` entry AND this loan's own
/// LoanReleased/InterestAdded loan_ledger rows in place — see
/// LoanOriginationEditedDomainEvent's doc comment for why revising the
/// loan_ledger rows too is safe now (SOA/ledger readers recompute a
/// chronological running balance instead of trusting the stamped one).
/// Skipping the origination-date correction here would otherwise leave the
/// SOA sorting a payment "before" the loan it belongs to whenever the true
/// disbursement date is edited to fall after some already-recorded
/// payments, producing a nonsensical negative running balance for those
/// rows even though the loan's overall Balance stays correct. Interest is
/// re-read from the loan's current TotalInterest (not carried on the event)
/// so this stays correct even when the same edit also changed the rate.
/// Loans edited before this feature existed still have a `loan_release`
/// entry (it's always created at origination via LoanCreatedEventHandler),
/// so the lookups should always find something — a missing row is
/// defensively tolerated, not thrown, so a valid loan edit is never blocked
/// by ledger housekeeping.
/// </summary>
public sealed class LoanOriginationEditedEventHandler : INotificationHandler<LoanOriginationEditedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanOriginationEditedEventHandler(
        ICashLedgerRepository cashLedgerRepository, ILoanLedgerRepository loanLedgerRepository,
        ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanOriginationEditedDomainEvent notification, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(notification.LoanId, ct);
        var loanNumber = loan is not null ? MappingExtensions.FormatLoanNumber(loan.LoanNumber) : notification.LoanId.ToString();

        var cashEntry = await _cashLedgerRepository.GetLoanReleaseEntryAsync(loanNumber, ct);
        cashEntry?.Revise(notification.NewPrincipal, notification.NewStartDate);

        var releaseEntry = await _loanLedgerRepository.GetByLoanIdAndTypeAsync(notification.LoanId, LoanLedgerTransactionType.LoanReleased, ct);
        releaseEntry?.ReviseDebit(notification.NewPrincipal, notification.NewStartDate);

        var interestEntry = loan is null ? null : await _loanLedgerRepository.GetByLoanIdAndTypeAsync(notification.LoanId, LoanLedgerTransactionType.InterestAdded, ct);
        interestEntry?.ReviseDebit(loan!.TotalInterest, notification.NewStartDate);

        if (cashEntry is not null || releaseEntry is not null || interestEntry is not null)
            await _unitOfWork.SaveChangesAsync(ct);
    }
}
