using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan extension being edited by revising the matching
/// loan_ledger row in place — see LoanExtensionEditedDomainEvent's doc
/// comment for why this is safe now. Extensions recorded before ReferenceId
/// tracking existed have no row to find here, so the lookup is allowed to
/// come back empty rather than throwing — this handler must not turn an
/// otherwise valid extension edit into a failed request.
/// </summary>
public sealed class LoanExtensionEditedEventHandler : INotificationHandler<LoanExtensionEditedDomainEvent>
{
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanExtensionEditedEventHandler(ILoanLedgerRepository loanLedgerRepository, IUnitOfWork unitOfWork)
    {
        _loanLedgerRepository = loanLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanExtensionEditedDomainEvent notification, CancellationToken ct)
    {
        var entry = await _loanLedgerRepository.GetByReferenceIdAsync(notification.LoanId, notification.ExtensionId.ToString(), ct);
        if (entry is null) return;

        entry.ReviseDebit(notification.NewAdditionalChargesAmount, notification.NewExtensionDate);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
