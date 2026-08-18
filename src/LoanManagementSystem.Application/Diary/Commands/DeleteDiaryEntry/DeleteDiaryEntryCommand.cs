using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Commands.DeleteDiaryEntry;

public sealed record DeleteDiaryEntryCommand(string DiaryEntryId, string DeletedBy) : IRequest;

public sealed class DeleteDiaryEntryCommandHandler : IRequestHandler<DeleteDiaryEntryCommand>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDiaryEntryCommandHandler(IDiaryRepository diaryRepository, IUnitOfWork unitOfWork)
    {
        _diaryRepository = diaryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDiaryEntryCommand request, CancellationToken ct)
    {
        var id = DiaryEntryId.Parse(request.DiaryEntryId);
        var entry = await _diaryRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiaryEntry), request.DiaryEntryId);

        entry.MarkForDeletion(request.DeletedBy);
        _diaryRepository.Remove(entry);

        await _unitOfWork.SaveChangesAsync(ct); // also flushes DiaryEntryDeletedDomainEvent → diary_audit_log
    }
}
