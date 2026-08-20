using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetDashboardReceivables;

public sealed record GetDashboardReceivablesQuery : IRequest<DashboardReceivablesDto>;

/// <summary>
/// Implements the spec's "Dashboard Receivable Calculations" directly:
///   Gross Receivables       = SUM(Balance) for every loan except Written Off
///   Bad Loan Receivables    = SUM(Balance) WHERE Classification = BadLoan
///   Collectible Receivables = Gross Receivables - Bad Loan Receivables
/// plus the "Dashboard Summary Cards" counts.
/// </summary>
public sealed class GetDashboardReceivablesQueryHandler : IRequestHandler<GetDashboardReceivablesQuery, DashboardReceivablesDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetDashboardReceivablesQueryHandler(ILoanRepository loanRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _appDateTime = appDateTime;
    }

    public async Task<DashboardReceivablesDto> Handle(GetDashboardReceivablesQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllAsync(ct);
        var today = _appDateTime.Today;

        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);

        var grossReceivables = loans
            .Where(l => l.Status != LoanStatus.WrittenOff)
            .Sum(l => l.Balance.Amount);

        var badLoanReceivables = loans
            .Where(l => l.Classification == LoanClassification.BadLoan)
            .Sum(l => l.Balance.Amount);

        var collectibleReceivables = grossReceivables - badLoanReceivables;

        var weekFromNow = today.AddDays(7);
        var loansDueThisWeek = loans.Count(l =>
            l.Status is LoanStatus.Active or LoanStatus.Extended or LoanStatus.Overdue &&
            l.DueDate >= today && l.DueDate <= weekFromNow);

        return new DashboardReceivablesDto(
            GrossReceivables: grossReceivables,
            CollectibleReceivables: collectibleReceivables,
            BadLoanReceivables: badLoanReceivables,
            ActiveLoansCount: loans.Count(l => l.Status is LoanStatus.Active or LoanStatus.Extended),
            OverdueLoansCount: loans.Count(l => l.Status == LoanStatus.Overdue),
            WrittenOffLoansCount: loans.Count(l => l.Status == LoanStatus.WrittenOff),
            LoansDueThisWeekCount: loansDueThisWeek
        );
    }
}
