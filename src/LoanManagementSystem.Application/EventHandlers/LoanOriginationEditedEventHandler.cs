using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan's Principal/StartDate being edited by revising the
/// matching cash_ledger `loan_release` entry in place — see
/// LoanOriginationEditedDomainEvent's doc comment for why this is scoped to
/// cash_ledger only. Loans edited before this feature existed still have a
/// `loan_release` entry (it's always created at origination via
/// LoanCreatedEventHandler), so the lookup-by-ReferenceId should always find
/// one — a missing row is defensively tolerated, not thrown, so a valid
/// loan edit is never blocked by ledger housekeeping.
/// </summary>
public sealed class LoanOriginationEditedEventHandler : INotificationHandler<LoanOriginationEditedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanOriginationEditedEventHandler(ICashLedgerRepository cashLedgerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanOriginationEditedDomainEvent notification, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(notification.LoanId, ct);
        var loanNumber = loan is not null ? MappingExtensions.FormatLoanNumber(loan.LoanNumber) : notification.LoanId.ToString();

        var entry = await _cashLedgerRepository.GetLoanReleaseEntryAsync(loanNumber, ct);
        if (entry is null) return;

        entry.Revise(notification.NewPrincipal, notification.NewStartDate);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
