using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.GetDiaryAuditLog;

/// <summary>Backs the Diary Entry Detail "Audit Information" section (requirements §13/§24) — see DiaryAuditLogEntry.</summary>
public sealed record GetDiaryAuditLogQuery(string DiaryEntryId) : IRequest<List<DiaryAuditLogEntryDto>>;

public sealed class GetDiaryAuditLogQueryHandler : IRequestHandler<GetDiaryAuditLogQuery, List<DiaryAuditLogEntryDto>>
{
    private readonly IDiaryAuditLogRepository _diaryAuditLogRepository;

    public GetDiaryAuditLogQueryHandler(IDiaryAuditLogRepository diaryAuditLogRepository)
    {
        _diaryAuditLogRepository = diaryAuditLogRepository;
    }

    public async Task<List<DiaryAuditLogEntryDto>> Handle(GetDiaryAuditLogQuery request, CancellationToken ct)
    {
        var entries = await _diaryAuditLogRepository.GetByDiaryEntryIdAsync(DiaryEntryId.Parse(request.DiaryEntryId), ct);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
