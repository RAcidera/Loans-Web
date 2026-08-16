using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan extension being deleted by removing its loan_ledger
/// row — see LoanExtensionDeletedDomainEvent's doc comment for why there's
/// no cash_ledger side to this (extensions never moved cash). Every
/// loan_ledger row created after the removed one has its stamped
/// RunningBalance shifted so the ledger's history stays internally
/// consistent with the loan's real, post-deletion Balance. Extensions
/// recorded before ReferenceId tracking existed have no row to find here,
/// so the lookup is allowed to come back empty rather than throwing — this
/// handler must not turn an otherwise valid extension deletion into a
/// failed request.
/// </summary>
public sealed class LoanExtensionDeletedEventHandler : INotificationHandler<LoanExtensionDeletedDomainEvent>
{
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanExtensionDeletedEventHandler(ILoanLedgerRepository loanLedgerRepository, IUnitOfWork unitOfWork)
    {
        _loanLedgerRepository = loanLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanExtensionDeletedDomainEvent notification, CancellationToken ct)
    {
        var loanEntry = await _loanLedgerRepository.GetByReferenceIdAsync(notification.LoanId, notification.ExtensionId.ToString(), ct);
        if (loanEntry is null)
            return;

        var laterEntries = await _loanLedgerRepository.GetAfterAsync(notification.LoanId, loanEntry.CreatedAtUtc, ct);
        var delta = loanEntry.Credit.Amount - loanEntry.Debit.Amount;
        foreach (var later in laterEntries)
            later.ShiftRunningBalance(delta);

        _loanLedgerRepository.Remove(loanEntry);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
