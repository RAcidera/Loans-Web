using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class DiaryEntryCreatedEventHandler : INotificationHandler<DiaryEntryCreatedDomainEvent>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiaryEntryCreatedEventHandler(IDiaryAuditLogRepository diaryAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DiaryEntryCreatedDomainEvent notification, CancellationToken ct)
    {
        _diaryAuditLogRepository.Add(DiaryAuditLogEntry.Record(
            notification.DiaryEntryId, DiaryAuditAction.Created, "Diary entry created.", notification.CreatedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
