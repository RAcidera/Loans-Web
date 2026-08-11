using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan extension by writing its LoanLedgerEntry row — no cash
/// moves (see LoanExtendedDomainEvent), but the SRS's own ledger example
/// shows an "Extension" line, so AdditionalInterestAmount and
/// AdditionalChargesAmount are combined into one Debit here rather than
/// split into two rows the way origination's Principal/Interest are.
/// </summary>
public sealed class LoanExtendedEventHandler : INotificationHandler<LoanExtendedDomainEvent>
{
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanExtendedEventHandler(ILoanLedgerRepository loanLedgerRepository, IUnitOfWork unitOfWork)
    {
        _loanLedgerRepository = loanLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanExtendedDomainEvent notification, CancellationToken ct)
    {
        var totalDebit = notification.AdditionalInterestAmount.Add(notification.AdditionalChargesAmount);

        _loanLedgerRepository.Add(LoanLedgerEntry.Record(
            notification.LoanId, LoanLedgerTransactionType.Extension,
            debit: totalDebit, credit: Money.Zero, runningBalance: notification.ResultingBalance,
            remarks: $"Extension +{notification.ExtensionDays} days", transactionDate: notification.ExtensionDate,
            referenceId: notification.ExtensionId.ToString()));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
