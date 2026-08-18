using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class PromiseKeptEventHandler : INotificationHandler<PromiseKeptDomainEvent>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromiseKeptEventHandler(IPromiseAuditLogRepository promiseAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PromiseKeptDomainEvent notification, CancellationToken ct)
    {
        _promiseAuditLogRepository.Add(PromiseAuditLogEntry.Record(
            notification.PromiseId, PromiseAuditAction.Kept, "Promise marked as kept.", notification.PerformedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
