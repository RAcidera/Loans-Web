using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Promises.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class PromiseRescheduledEventHandler : INotificationHandler<PromiseRescheduledDomainEvent>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromiseRescheduledEventHandler(IPromiseAuditLogRepository promiseAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PromiseRescheduledDomainEvent notification, CancellationToken ct)
    {
        _promiseAuditLogRepository.Add(PromiseAuditLogEntry.Record(
            notification.PromiseId, PromiseAuditAction.Rescheduled,
            $"Promise rescheduled to {notification.NewPromiseDate:yyyy-MM-dd}.", notification.PerformedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
