using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class DiaryReminderChangedEventHandler : INotificationHandler<DiaryReminderChangedDomainEvent>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiaryReminderChangedEventHandler(IDiaryAuditLogRepository diaryAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DiaryReminderChangedDomainEvent notification, CancellationToken ct)
    {
        _diaryAuditLogRepository.Add(DiaryAuditLogEntry.Record(
            notification.DiaryEntryId, DiaryAuditAction.ReminderChanged, "Reminder changed.", notification.ChangedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
