using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Diary;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Diary.Queries.CompareSnapshotToCurrent;

/// <summary>GET /api/diary/{id}/compare-to-today (requirements §15/§22) — throws DomainException (400) if the entry has no snapshot to compare against.</summary>
public sealed record CompareSnapshotToCurrentQuery(string DiaryEntryId) : IRequest<FinancialComparisonDto>;

public sealed class CompareSnapshotToCurrentQueryHandler : IRequestHandler<CompareSnapshotToCurrentQuery, FinancialComparisonDto>
{
    private readonly IDiaryRepository _diaryRepository;
    private readonly IFinancialSnapshotService _financialSnapshotService;
    private readonly IAppDateTimeService _appDateTime;

    public CompareSnapshotToCurrentQueryHandler(IDiaryRepository diaryRepository, IFinancialSnapshotService financialSnapshotService, IAppDateTimeService appDateTime)
    {
        _diaryRepository = diaryRepository;
        _financialSnapshotService = financialSnapshotService;
        _appDateTime = appDateTime;
    }

    public async Task<FinancialComparisonDto> Handle(CompareSnapshotToCurrentQuery request, CancellationToken ct)
    {
        var id = DiaryEntryId.Parse(request.DiaryEntryId);
        var entry = await _diaryRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiaryEntry), request.DiaryEntryId);

        var snapshot = entry.Snapshot
            ?? throw new NotFoundException(nameof(DiaryFinancialSnapshot), request.DiaryEntryId);

        var today = _appDateTime.Today;
        var current = await _financialSnapshotService.GetCurrentFinancialPositionAsync(today, ct);
        var since = await _financialSnapshotService.GetActivitySinceAsync(entry.EntryDate.AddDays(1), today, ct);

        var metrics = new List<FinancialComparisonMetricDto>
        {
            BuildMetric("grossReceivables", "Gross Receivables", snapshot.GrossReceivables, current.GrossReceivables),
            BuildMetric("collectibleReceivables", "Collectible Receivables", snapshot.CollectibleReceivables, current.CollectibleReceivables),
            BuildMetric("badLoanReceivables", "Bad Loan Receivables", snapshot.BadLoanReceivables, current.BadLoanReceivables),
            BuildMetric("cashOnHand", "Cash on Hand", snapshot.CashOnHand, current.CashOnHand),
            BuildMetric("totalBusinessPosition", "Total Business Position", snapshot.GrossReceivables + snapshot.CashOnHand, current.GrossReceivables + current.CashOnHand),
            BuildMetric("activeLoanCount", "Active Loans", snapshot.ActiveLoanCount, current.ActiveLoanCount),
            BuildMetric("overdueLoanCount", "Overdue Loans", snapshot.OverdueLoanCount, current.OverdueLoanCount),
            BuildMetric("badLoanCount", "Bad Loans", snapshot.BadLoanCount, current.BadLoanCount),
        };

        return new FinancialComparisonDto(
            SnapshotDate: entry.EntryDate.ToString("yyyy-MM-dd"),
            TodayDate: today.ToString("yyyy-MM-dd"),
            Metrics: metrics,
            CollectionsSinceSnapshot: since.Collections,
            LoanReleasesSinceSnapshot: since.LoanReleases);
    }

    /// <summary>Requirements §16 — Change = Current - Snapshot; Percentage = (Change / Snapshot) x 100, null (rendered "N/A"/"New" by the frontend) when SnapshotValue is zero.</summary>
    private static FinancialComparisonMetricDto BuildMetric(string key, string label, decimal snapshotValue, decimal currentValue)
    {
        var change = currentValue - snapshotValue;
        decimal? percentChange = snapshotValue == 0 ? null : Math.Round(change / snapshotValue * 100, 1);
        return new FinancialComparisonMetricDto(key, label, snapshotValue, currentValue, change, percentChange);
    }
}
