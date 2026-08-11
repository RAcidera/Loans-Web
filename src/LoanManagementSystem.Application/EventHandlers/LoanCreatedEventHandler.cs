using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a loan being disbursed by recording the corresponding
/// `loan_release` cash-out entry (SRS: "Transaction Type Effects") and the
/// loan's own ledger's opening two rows ("Loan Released" then "Interest
/// Added", matching the SRS's own ledger example for day-one origination).
/// Published by AppDbContext.SaveChangesAsync after the Loan itself has
/// been committed, and adding these entries triggers its own SaveChanges — see
/// the comment on AppDbContext for why that's a second database round trip
/// rather than the same one. Re-fetches the loan (rather than trusting a
/// LoanNumber captured at event-raise time) because LoanNumber is DB-assigned
/// on insert and is still 0 when Originate() raises this event — by the time
/// this handler runs, SaveChanges has already committed and the identity
/// value is readable, which a value captured earlier on the event wouldn't be.
/// </summary>
public sealed class LoanCreatedEventHandler : INotificationHandler<LoanCreatedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanCreatedEventHandler(ICashLedgerRepository cashLedgerRepository, ILoanLedgerRepository loanLedgerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanCreatedDomainEvent notification, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(notification.LoanId, ct);
        var loanNumber = loan is not null ? MappingExtensions.FormatLoanNumber(loan.LoanNumber) : notification.LoanId.ToString();

        var entry = CashLedgerEntry.Record(
            CashTransactionType.LoanRelease,
            notification.Principal,
            remarks: $"Loan release — {loanNumber}",
            transactionDate: notification.StartDate,
            referenceId: loanNumber);

        _cashLedgerRepository.Add(entry);

        _loanLedgerRepository.Add(LoanLedgerEntry.Record(
            notification.LoanId, LoanLedgerTransactionType.LoanReleased,
            debit: notification.Principal, credit: Money.Zero, runningBalance: notification.Principal,
            remarks: "Loan released", transactionDate: notification.StartDate));

        _loanLedgerRepository.Add(LoanLedgerEntry.Record(
            notification.LoanId, LoanLedgerTransactionType.InterestAdded,
            debit: notification.Interest, credit: Money.Zero, runningBalance: notification.Principal.Add(notification.Interest),
            remarks: "Interest added", transactionDate: notification.StartDate));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
