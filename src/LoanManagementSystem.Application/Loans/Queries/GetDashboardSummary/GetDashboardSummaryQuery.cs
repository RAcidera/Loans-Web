using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Financial;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

/// <summary>
/// Everything on the Dashboard beyond Cash on Hand (GetCashSummaryQuery)
/// and Recent Payments (GetRecentPaymentsQuery), which already had their
/// own endpoints: the top KPI row's month-over-month trend badges, the
/// Collections Overview/Past-7-Days charts, the Receivables Breakdown
/// donut, and the Recent Loans widget.
/// </summary>
public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetDashboardSummaryQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _appDateTime = appDateTime;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var today = _appDateTime.Today;
        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);

        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);

        var (gross, collectible, badLoan) = FinancialCalculations.ComputeReceivables(loans);

        var oneMonthAgo = today.AddMonths(-1);
        var priorCohort = loans.Where(l => l.StartDate <= oneMonthAgo).ToList();
        var (priorGross, priorCollectible, priorBadLoan) = FinancialCalculations.ComputeReceivables(priorCohort);
        var priorActiveCount = priorCohort.Count(l => l.Status is LoanStatus.Active or LoanStatus.Extended);
        var priorOverdueCount = priorCohort.Count(l => l.Status == LoanStatus.Overdue);

        var activeCount = loans.Count(l => l.Status is LoanStatus.Active or LoanStatus.Extended);
        var overdueCount = loans.Count(l => l.Status == LoanStatus.Overdue);
        var weekFromNow = today.AddDays(7);
        var dueThisWeekCount = loans.Count(l =>
            l.Status is LoanStatus.Active or LoanStatus.Extended or LoanStatus.Overdue &&
            l.DueDate >= today && l.DueDate <= weekFromNow);

        var customers = await _customerRepository.GetAllAsync(ct);
        var customerNames = customers.ToDictionary(c => c.Id, c => c.FullName);

        var recentLoans = loans
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(5)
            .Select(l => l.ToDto(customerNames.GetValueOrDefault(l.CustomerId, "Unknown")))
            .ToList();

        return new DashboardSummaryDto(
            GrossReceivables: gross,
            GrossReceivablesChangePercent: PercentChange(gross, priorGross),
            CollectibleReceivables: collectible,
            CollectibleReceivablesChangePercent: PercentChange(collectible, priorCollectible),
            BadLoanReceivables: badLoan,
            BadLoanReceivablesChangePercent: PercentChange(badLoan, priorBadLoan),
            ActiveLoansCount: activeCount,
            ActiveLoansChangePercent: PercentChange(activeCount, priorActiveCount),
            OverdueLoansCount: overdueCount,
            OverdueLoansChangePercent: PercentChange(overdueCount, priorOverdueCount),
            LoansDueThisWeekCount: dueThisWeekCount,
            MonthlyCollections: BuildMonthlyCollections(loans, today),
            Last7DaysCollections: BuildLast7DaysCollections(loans, today),
            ReceivablesBreakdown: BuildReceivablesBreakdown(loans),
            RecentLoans: recentLoans
        );
    }

    /// <summary>
    /// Loans and Customers have no historical status/balance snapshots, so
    /// a true point-in-time "last month" figure isn't reconstructable —
    /// this instead compares today's whole-portfolio totals against the
    /// same totals computed over just the loans that already existed a
    /// month ago (StartDate at least a month old), using their current
    /// balances/status. That's a real, honestly-computed month-over-month
    /// portfolio-growth signal, not a fabricated number, but it isn't the
    /// same thing as "what these totals actually were a month ago."
    /// </summary>
    private static decimal? PercentChange(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0m : null;
        return Math.Round((current - previous) / previous * 100, 1);
    }

    private static List<MonthlyCollectionDto> BuildMonthlyCollections(List<Loan> loans, DateOnly today)
    {
        var payments = loans.SelectMany(l => l.Payments).ToList();
        var thisYear = today.Year;
        var lastYear = thisYear - 1;

        var months = new List<MonthlyCollectionDto>();
        for (var month = 1; month <= 12; month++)
        {
            var monthName = new DateOnly(thisYear, month, 1).ToString("MMM");
            var thisYearTotal = payments.Where(p => p.PaymentDate.Year == thisYear && p.PaymentDate.Month == month).Sum(p => p.AmountPaid.Amount);
            var lastYearTotal = payments.Where(p => p.PaymentDate.Year == lastYear && p.PaymentDate.Month == month).Sum(p => p.AmountPaid.Amount);
            months.Add(new MonthlyCollectionDto(monthName, thisYearTotal, lastYearTotal));
        }

        return months;
    }

    private static List<DailyCollectionDto> BuildLast7DaysCollections(List<Loan> loans, DateOnly today)
    {
        var payments = loans.SelectMany(l => l.Payments).ToList();
        var days = new List<DailyCollectionDto>();

        for (var i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var total = payments.Where(p => p.PaymentDate == date).Sum(p => p.AmountPaid.Amount);
            days.Add(new DailyCollectionDto(date.ToString("yyyy-MM-dd"), total));
        }

        return days;
    }

    /// <summary>
    /// Current/Overdue/Bad Loan use Balance (outstanding), the same figure
    /// Gross Receivables sums — so those three slices always add up to
    /// exactly the Gross Receivables KPI above the chart, not some larger,
    /// unrelated total. Paid loans have zero balance by definition, so
    /// they'd otherwise be an always-empty slice; TotalPaid instead shows
    /// how much was actually collected on them, as an explicit "already
    /// settled, no longer a receivable" slice alongside the other three.
    /// </summary>
    private static ReceivablesBreakdownDto BuildReceivablesBreakdown(List<Loan> loans)
    {
        var current = loans.Where(l => l.Status is LoanStatus.Active or LoanStatus.Extended && l.Classification != LoanClassification.BadLoan);
        var overdue = loans.Where(l => l.Status == LoanStatus.Overdue && l.Classification != LoanClassification.BadLoan);
        var badLoan = loans.Where(l => l.Classification == LoanClassification.BadLoan && l.Status != LoanStatus.WrittenOff);
        var paid = loans.Where(l => l.Status == LoanStatus.Paid);

        return new ReceivablesBreakdownDto(
            Current: current.Sum(l => l.Balance.Amount),
            Overdue: overdue.Sum(l => l.Balance.Amount),
            BadLoan: badLoan.Sum(l => l.Balance.Amount),
            Paid: paid.Sum(l => l.TotalPaid.Amount)
        );
    }
}
