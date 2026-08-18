using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.GetDiaryFinancialSnapshot;

/// <summary>GET /api/diary/{id}/snapshot (requirements §22) — null when the entry has no snapshot (the checkbox wasn't set at creation), which the controller turns into 404.</summary>
public sealed record GetDiaryFinancialSnapshotQuery(string DiaryEntryId) : IRequest<DiaryFinancialSnapshotDto?>;

public sealed class GetDiaryFinancialSnapshotQueryHandler : IRequestHandler<GetDiaryFinancialSnapshotQuery, DiaryFinancialSnapshotDto?>
{
    private readonly IDiaryRepository _diaryRepository;

    public GetDiaryFinancialSnapshotQueryHandler(IDiaryRepository diaryRepository)
    {
        _diaryRepository = diaryRepository;
    }

    public async Task<DiaryFinancialSnapshotDto?> Handle(GetDiaryFinancialSnapshotQuery request, CancellationToken ct)
    {
        var id = DiaryEntryId.Parse(request.DiaryEntryId);
        var entry = await _diaryRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiaryEntry), request.DiaryEntryId);

        return entry.Snapshot?.ToDto();
    }
}
