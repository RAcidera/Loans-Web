using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetInterestSummary;

public sealed record GetInterestSummaryQuery : IRequest<InterestSummaryDto>;

/// <summary>
/// SRS 3.5's "total interest earned" — previously a documented gap. Sums
/// Loan.TotalInterest for every loan that isn't overdue-only noise: Paid
/// loans have fully realized their interest, and Active/Extended loans have
/// it accruing/booked at origination (this system's interest is flat, not
/// compounding, so the full amount is known up front — see InterestRate).
/// </summary>
public sealed class GetInterestSummaryQueryHandler : IRequestHandler<GetInterestSummaryQuery, InterestSummaryDto>
{
    private readonly ILoanRepository _loanRepository;

    public GetInterestSummaryQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<InterestSummaryDto> Handle(GetInterestSummaryQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllAsync(ct);

        var totalInterest = loans
            .Where(l => l.Status is LoanStatus.Paid or LoanStatus.Active or LoanStatus.Extended)
            .Sum(l => l.TotalInterest.Amount);

        return new InterestSummaryDto(totalInterest);
    }
}
