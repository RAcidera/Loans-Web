using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.GetDiaryCategories;

/// <summary>GET /api/diary-categories (requirements §22) — active categories only, backs the Diary form's dropdown.</summary>
public sealed record GetDiaryCategoriesQuery : IRequest<List<DiaryCategoryDto>>;

public sealed class GetDiaryCategoriesQueryHandler : IRequestHandler<GetDiaryCategoriesQuery, List<DiaryCategoryDto>>
{
    private readonly IDiaryCategoryRepository _categoryRepository;

    public GetDiaryCategoriesQueryHandler(IDiaryCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<DiaryCategoryDto>> Handle(GetDiaryCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _categoryRepository.GetActiveAsync(ct);
        return categories.Select(c => c.ToDto()).ToList();
    }
}
