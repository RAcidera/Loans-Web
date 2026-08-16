using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>
/// Reacts to a payment being recorded by creating the `payment_received`
/// cash-in entry the SRS requires: "Each payment automatically creates a
/// payment_received entry in the Cash_Ledger table." This is the concrete
/// resolution of the cross-aggregate consistency gap the Angular mock
/// repository could only leave as a comment — Loan and CashLedgerEntry
/// stay two separate aggregates/repositories, connected only by this event,
/// never by one repository calling another directly. Also writes this
/// payment's LoanLedgerEntry row (Credit side) — ReferenceId is the
/// PaymentId so the Payments tab can look up this row's RunningBalance
/// directly instead of assuming ledger order matches table order.
/// Re-fetches the loan for its LoanNumber (see LoanCreatedEventHandler's
/// comment — a value captured at event-raise time can't be trusted for a
/// loan that hasn't been saved yet, e.g. DbSeeder recording a payment
/// before its first SaveChanges).
/// </summary>
public sealed class PaymentRecordedEventHandler : INotificationHandler<PaymentRecordedDomainEvent>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentRecordedEventHandler(ICashLedgerRepository cashLedgerRepository, ILoanLedgerRepository loanLedgerRepository, ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PaymentRecordedDomainEvent notification, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(notification.LoanId, ct);
        var loanNumber = loan is not null ? MappingExtensions.FormatLoanNumber(loan.LoanNumber) : notification.LoanId.ToString();

        var entry = CashLedgerEntry.Record(
            CashTransactionType.PaymentReceived,
            notification.AmountPaid,
            remarks: $"Payment received — loan {loanNumber}",
            transactionDate: notification.PaymentDate,
            referenceId: loanNumber,
            sourcePaymentId: notification.PaymentId);

        _cashLedgerRepository.Add(entry);

        _loanLedgerRepository.Add(LoanLedgerEntry.Record(
            notification.LoanId, LoanLedgerTransactionType.Payment,
            debit: Money.Zero, credit: notification.AmountPaid, runningBalance: notification.ResultingBalance,
            remarks: "Payment received", transactionDate: notification.PaymentDate,
            referenceId: notification.PaymentId.ToString()));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
