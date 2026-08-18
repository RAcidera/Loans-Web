using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class DiaryLinkedCustomerChangedEventHandler : INotificationHandler<DiaryLinkedCustomerChangedDomainEvent>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiaryLinkedCustomerChangedEventHandler(IDiaryAuditLogRepository diaryAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DiaryLinkedCustomerChangedDomainEvent notification, CancellationToken ct)
    {
        _diaryAuditLogRepository.Add(DiaryAuditLogEntry.Record(
            notification.DiaryEntryId, DiaryAuditAction.LinkedCustomerChanged, "Linked customer changed.", notification.ChangedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
