using LoanManagementSystem.Application.Common.Financial;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;

namespace LoanManagementSystem.Application.Diary;

/// <summary>
/// The one place that assembles a full "current financial position" out of
/// FinancialCalculations' individual formulas (requirements §9: reuse the
/// same calculation services the Dashboard and Reports use, never
/// re-derive a formula in the Diary module). Used both when capturing a
/// new snapshot (CreateDiaryEntryCommandHandler) and when comparing an
/// existing one to today (CompareSnapshotToCurrentQueryHandler) — both
/// paths get the exact same numbers for "today" that the Dashboard would
/// show at that instant.
/// </summary>
public sealed class FinancialSnapshotService : IFinancialSnapshotService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICashLedgerRepository _cashLedgerRepository;

    public FinancialSnapshotService(ILoanRepository loanRepository, ICashLedgerRepository cashLedgerRepository)
    {
        _loanRepository = loanRepository;
        _cashLedgerRepository = cashLedgerRepository;
    }

    public async Task<FinancialPositionSnapshot> GetCurrentFinancialPositionAsync(DateOnly asOfDate, CancellationToken ct = default)
    {
        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(asOfDate);

        var (gross, collectible, badLoan) = FinancialCalculations.ComputeReceivables(loans);

        var activeCount = loans.Count(l => l.Status is LoanStatus.Active or LoanStatus.Extended);
        var overdueCount = loans.Count(l => l.Status == LoanStatus.Overdue);
        var badLoanCount = loans.Count(l => l.Classification == LoanClassification.BadLoan && l.Status != LoanStatus.WrittenOff);

        var monthStart = new DateOnly(asOfDate.Year, asOfDate.Month, 1);
        var payments = loans.SelectMany(l => l.Payments).ToList();
        var collectionsToday = FinancialCalculations.ComputeCollections(payments, asOfDate, asOfDate);
        var collectionsMtd = FinancialCalculations.ComputeCollections(payments, monthStart, asOfDate);

        var releasesToday = FinancialCalculations.ComputeLoanReleases(loans, asOfDate, asOfDate);
        var releasesMtd = FinancialCalculations.ComputeLoanReleases(loans, monthStart, asOfDate);

        var cashEntries = await _cashLedgerRepository.GetAllAsync(ct);
        var cashOnHand = FinancialCalculations.ComputeCashOnHand(cashEntries, asOfDate);

        return new FinancialPositionSnapshot(
            gross, collectible, badLoan, cashOnHand,
            activeCount, overdueCount, badLoanCount,
            collectionsToday, collectionsMtd, releasesToday, releasesMtd);
    }
}
