using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class PromiseCreatedEventHandler : INotificationHandler<PromiseCreatedDomainEvent>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromiseCreatedEventHandler(IPromiseAuditLogRepository promiseAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PromiseCreatedDomainEvent notification, CancellationToken ct)
    {
        _promiseAuditLogRepository.Add(PromiseAuditLogEntry.Record(
            notification.PromiseId, PromiseAuditAction.Created, "Promise to pay created.", notification.CreatedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
