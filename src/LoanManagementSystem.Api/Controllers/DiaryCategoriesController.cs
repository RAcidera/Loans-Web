using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Diary.Queries.GetDiaryCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/diary-categories")]
[Authorize]
public class DiaryCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiaryCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/diary-categories — active categories, ordered by SortOrder, for the Diary form's category dropdown (requirements §5/§6).</summary>
    [HttpGet]
    public async Task<ActionResult<List<DiaryCategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDiaryCategoriesQuery(), ct));
}
