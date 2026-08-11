using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>Reacts to a loan being edited post-creation (goodwill discount, data correction) by writing an audit-log entry — see LoanAuditLogEntry.</summary>
public sealed class LoanEditedEventHandler : INotificationHandler<LoanEditedDomainEvent>
{
    private readonly ILoanAuditLogRepository _loanAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanEditedEventHandler(ILoanAuditLogRepository loanAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _loanAuditLogRepository = loanAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanEditedDomainEvent notification, CancellationToken ct)
    {
        _loanAuditLogRepository.Add(LoanAuditLogEntry.Record(
            notification.LoanId, LoanAuditAction.Edited, "Loan details edited.", notification.EditedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
