using LoanManagementSystem.Domain.Diary;

namespace LoanManagementSystem.Domain.Repositories;

public interface IDiaryCategoryRepository
{
    Task<DiaryCategory?> GetByIdAsync(DiaryCategoryId id, CancellationToken ct = default);

    /// <summary>Active categories only, ordered by SortOrder — backs the Diary form's category dropdown (requirements §5/§6).</summary>
    Task<List<DiaryCategory>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Every category regardless of IsActive, ordered by SortOrder — used to resolve display fields (name/icon/color) for existing diary entries, which may reference a category that's since been deactivated.</summary>
    Task<List<DiaryCategory>> GetAllAsync(CancellationToken ct = default);

    void Add(DiaryCategory category);
}
