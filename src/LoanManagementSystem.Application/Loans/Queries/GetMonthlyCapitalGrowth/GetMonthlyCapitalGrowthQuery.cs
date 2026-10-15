using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Financial;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetMonthlyCapitalGrowth;

public sealed record GetMonthlyCapitalGrowthQuery(int Months = 12) : IRequest<MonthlyCapitalGrowthDto>;

/// <summary>
/// The Dashboard's "Monthly Capital Growth" panel. Read-only: loads both
/// ledgers, the loans (for the drift check and loan numbers), and the
/// WrittenOff audit rows once each, hands them to CapitalGrowthCalculator,
/// then reconciles the result against the live Total Business Position
/// formula and any Diary Financial Snapshots in range. Reconciliation
/// differences are reported, never applied.
/// </summary>
public sealed class GetMonthlyCapitalGrowthQueryHandler : IRequestHandler<GetMonthlyCapitalGrowthQuery, MonthlyCapitalGrowthDto>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanAuditLogRepository _loanAuditLogRepository;
    private readonly IDiaryRepository _diaryRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetMonthlyCapitalGrowthQueryHandler(
        ICashLedgerRepository cashLedgerRepository,
        ILoanLedgerRepository loanLedgerRepository,
        ILoanRepository loanRepository,
        ILoanAuditLogRepository loanAuditLogRepository,
        IDiaryRepository diaryRepository,
        IAppDateTimeService appDateTime)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _loanRepository = loanRepository;
        _loanAuditLogRepository = loanAuditLogRepository;
        _diaryRepository = diaryRepository;
        _appDateTime = appDateTime;
    }

    public async Task<MonthlyCapitalGrowthDto> Handle(GetMonthlyCapitalGrowthQuery request, CancellationToken ct)
    {
        if (!CapitalGrowthCalculator.AllowedMonthRanges.Contains(request.Months))
            throw new DomainException($"Months must be one of {string.Join(", ", CapitalGrowthCalculator.AllowedMonthRanges.Order())}.");

        var today = _appDateTime.Today;
        var cashEntries = await _cashLedgerRepository.GetAllAsync(ct);
        var loanRows = await _loanLedgerRepository.GetAllAsync(ct);
        var loans = await _loanRepository.GetAllAsync(ct);
        var writeOffRows = await _loanAuditLogRepository.GetByActionAsync(LoanAuditAction.WrittenOff, ct);

        // The audit row's UTC instant → the business-local date it happened on.
        var writeOffDates = writeOffRows
            .GroupBy(e => e.LoanId)
            .ToDictionary(g => g.Key, g => DateOnly.FromDateTime(_appDateTime.ConvertUtcToBusinessLocal(g.Max(e => e.OccurredAtUtc))));

        var loanInfos = loans
            .Select(l => new CapitalGrowthLoanInfo(
                l.Id,
                MappingExtensions.FormatLoanNumber(l.LoanNumber),
                l.Balance.Amount,
                l.Status == LoanStatus.WrittenOff,
                writeOffDates.TryGetValue(l.Id, out var date) ? date : null))
            .ToList();

        var result = CapitalGrowthCalculator.Compute(cashEntries, loanRows, loanInfos, today, request.Months);

        var dashboardPosition = FinancialCalculations.ComputeReceivables(loans).Gross + FinancialCalculations.ComputeCashOnHand(cashEntries);
        var currentEnding = result.Months[^1].EndingPosition;
        var difference = currentEnding is { } ending ? dashboardPosition - ending : (decimal?)null;

        var rangeStartUtc = _appDateTime.GetStartOfBusinessDayUtc(result.Months[0].Start);
        var rangeEndUtc = _appDateTime.GetStartOfBusinessDayUtc(today.AddDays(1));
        var snapshots = await _diaryRepository.GetSnapshotsCapturedBetweenAsync(rangeStartUtc, rangeEndUtc, ct);
        var snapshotChecks = snapshots
            .Select(s =>
            {
                var businessDate = DateOnly.FromDateTime(_appDateTime.ConvertUtcToBusinessLocal(s.SnapshotDateTimeUtc));
                var (cash, gross) = CapitalGrowthCalculator.ComputePositionAt(cashEntries, loanRows, loanInfos, businessDate, today);
                var snapshotPosition = s.CashOnHand + s.GrossReceivables;
                return new SnapshotReconciliationDto(s.SnapshotDateTimeUtc, businessDate.ToString("yyyy-MM-dd"), snapshotPosition, cash + gross, snapshotPosition - (cash + gross));
            })
            .ToList();

        return new MonthlyCapitalGrowthDto(
            TimeZone: _appDateTime.BusinessTimeZoneId,
            AsOfUtc: _appDateTime.UtcNow,
            AsOfDate: today.ToString("yyyy-MM-dd"),
            Currency: "PHP",
            Months: request.Months,
            HistoryBasis: "restated",
            HistoryStartDate: result.HistoryStartDate?.ToString("yyyy-MM-dd"),
            Series: result.Months.Select(m => m.ToDto()).ToList(),
            Summary: result.Summary.ToDto(),
            Reconciliation: new CapitalGrowthReconciliationDto(dashboardPosition, currentEnding, difference, difference == 0m, snapshotChecks),
            Warnings: result.Warnings.Select(w => w.ToDto()).ToList()
        );
    }
}
