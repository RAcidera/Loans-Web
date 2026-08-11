using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Loans.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

/// <summary>Reacts to a loan write-off by writing an audit-log entry — see LoanAuditLogEntry.</summary>
public sealed class LoanWrittenOffEventHandler : INotificationHandler<LoanWrittenOffDomainEvent>
{
    private readonly ILoanAuditLogRepository _loanAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoanWrittenOffEventHandler(ILoanAuditLogRepository loanAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _loanAuditLogRepository = loanAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LoanWrittenOffDomainEvent notification, CancellationToken ct)
    {
        _loanAuditLogRepository.Add(LoanAuditLogEntry.Record(
            notification.LoanId, LoanAuditAction.WrittenOff, "Loan written off.", notification.WrittenOffBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
