using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class PromiseUpdatedEventHandler : INotificationHandler<PromiseUpdatedDomainEvent>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromiseUpdatedEventHandler(IPromiseAuditLogRepository promiseAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PromiseUpdatedDomainEvent notification, CancellationToken ct)
    {
        _promiseAuditLogRepository.Add(PromiseAuditLogEntry.Record(
            notification.PromiseId, PromiseAuditAction.Updated, "Promise to pay edited.", notification.EditedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
