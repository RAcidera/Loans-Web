using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.EventHandlers;

public sealed class DiaryLinkedLoanChangedEventHandler : INotificationHandler<DiaryLinkedLoanChangedDomainEvent>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiaryLinkedLoanChangedEventHandler(IDiaryAuditLogRepository diaryAuditLogRepository, IUnitOfWork unitOfWork)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DiaryLinkedLoanChangedDomainEvent notification, CancellationToken ct)
    {
        _diaryAuditLogRepository.Add(DiaryAuditLogEntry.Record(
            notification.DiaryEntryId, DiaryAuditAction.LinkedLoanChanged, "Linked loan changed.", notification.ChangedBy));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
