using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class DiaryEntryDeletedEventHandler : INotificationHandler<DiaryEntryDeletedDomainEvent>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiaryEntryDeletedEventHandler(IDiaryAuditLogRepository diaryAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DiaryEntryDeletedDomainEvent notification, CancellationToken ct)
    {
        _diaryAuditLogRepository.Add(DiaryAuditLogEntry.Record(
            notification.DiaryEntryId, DiaryAuditAction.Deleted, "Diary entry deleted.", notification.DeletedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
