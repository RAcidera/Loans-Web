using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>Reacts to a classification change by writing an audit-log entry — see LoanAuditLogEntry.</summary>
public sealed class LoanClassificationChangedEventHandler : INotificationHandler<LoanClassificationChangedDomainEvent>
{
    private readonly ILoanAuditLogRepository _loanAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanClassificationChangedEventHandler(ILoanAuditLogRepository loanAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _loanAuditLogRepository = loanAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanClassificationChangedDomainEvent notification, CancellationToken ct)
    {
        var description = $"Classification changed from {notification.OldClassification} to {notification.NewClassification}.";
        _loanAuditLogRepository.Add(LoanAuditLogEntry.Record(
            notification.LoanId, LoanAuditAction.ClassificationChanged, description, notification.ChangedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
