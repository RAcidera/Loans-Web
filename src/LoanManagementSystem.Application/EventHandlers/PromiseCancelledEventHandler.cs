using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class PromiseCancelledEventHandler : INotificationHandler<PromiseCancelledDomainEvent>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromiseCancelledEventHandler(IPromiseAuditLogRepository promiseAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PromiseCancelledDomainEvent notification, CancellationToken ct)
    {
        _promiseAuditLogRepository.Add(PromiseAuditLogEntry.Record(
            notification.PromiseId, PromiseAuditAction.Cancelled, "Promise cancelled.", notification.PerformedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
