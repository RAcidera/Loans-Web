using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.GetDiarySummary;

/// <summary>GET /api/diary/summary (requirements diary-modern §5/§20) — the Diary page's Summary Cards and right-sidebar Quick Summary/Category Summary, computed fresh on every request rather than cached.</summary>
public sealed record GetDiarySummaryQuery : IRequest<DiarySummaryDto>;

public sealed class GetDiarySummaryQueryHandler : IRequestHandler<GetDiarySummaryQuery, DiarySummaryDto>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _categoryRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IFinancialSnapshotService _financialSnapshotService;

    public GetDiarySummaryQueryHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository categoryRepository,
        ILoanRepository loanRepository, IFinancialSnapshotService financialSnapshotService)
    {
        _diaryRepository = diaryRepository;
        _categoryRepository = categoryRepository;
        _loanRepository = loanRepository;
        _financialSnapshotService = financialSnapshotService;
    }

    public async Task<DiarySummaryDto> Handle(GetDiarySummaryQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = await _diaryRepository.SearchAsync(new DiarySearchFilters(), ct);

        var totalEntries = entries.Count;
        var entriesThisMonth = entries.Count(e => e.EntryDate.Year == today.Year && e.EntryDate.Month == today.Month);
        var pendingReminders = entries.Count(e => e.ReminderDate is { } reminderDate && reminderDate >= today);

        var categories = await _categoryRepository.GetActiveAsync(ct);
        var countByCategory = entries.GroupBy(e => e.CategoryId).ToDictionary(g => g.Key, g => g.Count());
        var categoryCounts = categories
            .Select(c => new DiaryCategoryCountDto(c.Id.ToString(), c.Name, c.Icon, c.DisplayColor, countByCategory.GetValueOrDefault(c.Id, 0)))
            .ToList();

        // "Loan Due" mirrors the Calendar module's monthly Loan Due/Extension
        // Due event sources (GetCalendarEventsQueryHandler) — every loan
        // still open (Active/Extended/Overdue) whose current due date falls
        // within this calendar month, combined into one count here rather
        // than split by type since the Diary summary card has no room for
        // both.
        var loans = await _loanRepository.GetAllAsync(ct);
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);
        var loanDueCount = loans.Count(l =>
            l.Status is LoanStatus.Active or LoanStatus.Extended or LoanStatus.Overdue &&
            l.DueDate.Year == today.Year && l.DueDate.Month == today.Month);

        var position = await _financialSnapshotService.GetCurrentFinancialPositionAsync(today, ct);

        return new DiarySummaryDto(
            TotalEntries: totalEntries,
            EntriesThisMonth: entriesThisMonth,
            LoanDueCount: loanDueCount,
            PendingRemindersCount: pendingReminders,
            CollectionsToday: position.CollectionsToday,
            CollectionsMonthToDate: position.CollectionsMonthToDate,
            LoanReleasesToday: position.LoanReleasesToday,
            LoanReleasesMonthToDate: position.LoanReleasesMonthToDate,
            CategoryCounts: categoryCounts);
    }
}
