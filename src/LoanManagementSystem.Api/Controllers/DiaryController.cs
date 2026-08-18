using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Diary.Commands.CreateDiaryEntry;
using LoanManagementSystem.Application.Diary.Commands.DeleteDiaryEntry;
using LoanManagementSystem.Application.Diary.Commands.UpdateDiaryEntry;
using LoanManagementSystem.Application.Diary.Queries.CompareSnapshotToCurrent;
using LoanManagementSystem.Application.Diary.Queries.GetDiaryAuditLog;
using LoanManagementSystem.Application.Diary.Queries.GetDiaryEntry;
using LoanManagementSystem.Application.Diary.Queries.GetDiaryFinancialSnapshot;
using LoanManagementSystem.Application.Diary.Queries.GetDiarySummary;
using LoanManagementSystem.Application.Diary.Queries.SearchDiaryEntries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagementSystem.Api.Controllers;

[ApiController]
[Route("api/diary")]
[Authorize] // any authenticated user (Admin or Staff)
public class DiaryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiaryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/diary?search=&amp;categoryId=&amp;dateFrom=&amp;dateTo=&amp;customerId=&amp;loanId=&amp;hasSnapshot=&amp;hasReminder= — the Diary timeline (requirements §11/§12), sorted EntryDateTime DESC, not paged.</summary>
    [HttpGet]
    public async Task<ActionResult<List<DiaryEntryDto>>> Search(
        [FromQuery] string? search = null, [FromQuery] string? categoryId = null,
        [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null,
        [FromQuery] string? customerId = null, [FromQuery] string? loanId = null,
        [FromQuery] bool? hasSnapshot = null, [FromQuery] bool? hasReminder = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new SearchDiaryEntriesQuery(search, categoryId, dateFrom, dateTo, customerId, loanId, hasSnapshot, hasReminder), ct));

    /// <summary>GET /api/diary/summary — the Diary page's Summary Cards and right-sidebar Quick Summary/Category Summary (requirements diary-modern §5/§20).</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DiarySummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDiarySummaryQuery(), ct));

    /// <summary>GET /api/diary/{id} — the Diary Entry Detail page (requirements §13).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DiaryEntryDto>> GetById(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDiaryEntryQuery(id), ct));

    /// <summary>GET /api/diary/{id}/snapshot — requirements §14/§22.</summary>
    [HttpGet("{id}/snapshot")]
    public async Task<ActionResult<DiaryFinancialSnapshotDto>> GetSnapshot(string id, CancellationToken ct)
    {
        var snapshot = await _mediator.Send(new GetDiaryFinancialSnapshotQuery(id), ct);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    /// <summary>GET /api/diary/{id}/compare-to-today — requirements §15/§22.</summary>
    [HttpGet("{id}/compare-to-today")]
    public async Task<ActionResult<FinancialComparisonDto>> CompareToToday(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new CompareSnapshotToCurrentQuery(id), ct));

    /// <summary>GET /api/diary/{id}/audit-log — requirements §13/§24.</summary>
    [HttpGet("{id}/audit-log")]
    public async Task<ActionResult<List<DiaryAuditLogEntryDto>>> GetAuditLog(string id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDiaryAuditLogQuery(id), ct));

    /// <summary>POST /api/diary — requirements §6/§7. CreatedBy comes from the authenticated user, not the request body.</summary>
    [HttpPost]
    public async Task<ActionResult<DiaryEntryDto>> Create(CreateDiaryEntryRequest request, CancellationToken ct)
    {
        var command = new CreateDiaryEntryCommand(
            request.Title, request.CategoryId, request.Notes, request.CaptureFinancialSnapshot,
            User.Identity?.Name ?? "unknown",
            request.CustomerId, request.LoanId, request.EntryDate, request.EntryTime, request.ReminderDate, request.ReminderTime, request.Tags);

        var created = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.DiaryEntryId }, created);
    }

    /// <summary>PUT /api/diary/{id} — requirements §13's editable fields; never the financial snapshot.</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DiaryEntryDto>> Update(string id, UpdateDiaryEntryRequest request, CancellationToken ct)
    {
        var command = new UpdateDiaryEntryCommand(
            id, request.Title, request.CategoryId, request.Notes, User.Identity?.Name ?? "unknown",
            request.CustomerId, request.LoanId, request.EntryDate, request.EntryTime, request.ReminderDate, request.ReminderTime, request.Tags);

        return Ok(await _mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/diary/{id}.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDiaryEntryCommand(id, User.Identity?.Name ?? "unknown"), ct);
        return NoContent();
    }
}

public sealed record CreateDiaryEntryRequest(
    string Title, string CategoryId, string Notes, bool CaptureFinancialSnapshot,
    string? CustomerId = null, string? LoanId = null, string? EntryDate = null, string? EntryTime = null,
    string? ReminderDate = null, string? ReminderTime = null, string? Tags = null);

public sealed record UpdateDiaryEntryRequest(
    string Title, string CategoryId, string Notes,
    string? CustomerId = null, string? LoanId = null, string? EntryDate = null, string? EntryTime = null,
    string? ReminderDate = null, string? ReminderTime = null, string? Tags = null);
