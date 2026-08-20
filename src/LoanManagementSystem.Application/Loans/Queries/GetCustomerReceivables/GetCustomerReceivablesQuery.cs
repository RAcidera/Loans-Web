using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetCustomerReceivables;

public sealed record GetCustomerReceivablesQuery(string CustomerId) : IRequest<DashboardReceivablesDto>;

/// <summary>
/// The Customer Profile "Financial Summary" card — same Gross/Collectible/
/// Bad Loan Receivables formulas as GetDashboardReceivablesQueryHandler,
/// filtered to one customer's loans instead of the whole portfolio.
/// </summary>
public sealed class GetCustomerReceivablesQueryHandler : IRequestHandler<GetCustomerReceivablesQuery, DashboardReceivablesDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetCustomerReceivablesQueryHandler(ILoanRepository loanRepository, IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _appDateTime = appDateTime;
    }

    public async Task<DashboardReceivablesDto> Handle(GetCustomerReceivablesQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetByCustomerAsync(CustomerId.Parse(request.CustomerId), ct);
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
