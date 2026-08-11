using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetPeriodSummary;

public sealed record GetPeriodSummaryQuery(DateOnly StartDate, DateOnly EndDate) : IRequest<PeriodSummaryDto>;

/// <summary>
/// SRS 3.5's "summaries per period": loans originated, payments collected,
/// extensions granted, and interest earned, all filtered to [StartDate,
/// EndDate] inclusive. Loads every loan with its Payments/Extensions
/// included and aggregates in memory — the same "load it all, sum in C#"
/// approach GetCashSummaryQueryHandler uses, appropriate at this system's
/// scale (SRS 4: "hundreds of loans").
/// </summary>
public sealed class GetPeriodSummaryQueryHandler : IRequestHandler<GetPeriodSummaryQuery, PeriodSummaryDto>
{
    private readonly ILoanRepository _loanRepository;

    public GetPeriodSummaryQueryHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<PeriodSummaryDto> Handle(GetPeriodSummaryQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);

        bool InRange(DateOnly date) => date >= request.StartDate && date <= request.EndDate;

        var loansOriginated = loans.Where(l => InRange(l.StartDate)).ToList();

        var paymentsCollected = loans
            .SelectMany(l => l.Payments)
            .Where(p => InRange(p.PaymentDate))
            .Sum(p => p.AmountPaid.Amount);

        var extensionsGranted = loans
            .SelectMany(l => l.Extensions)
            .Count(e => InRange(e.ExtensionDate));

        return new PeriodSummaryDto(
            StartDate: request.StartDate.ToString("yyyy-MM-dd"),
            EndDate: request.EndDate.ToString("yyyy-MM-dd"),
            LoansOriginated: loansOriginated.Count,
            PaymentsCollected: paymentsCollected,
            ExtensionsGranted: extensionsGranted,
            InterestEarned: loansOriginated.Sum(l => l.TotalInterest.Amount)
        );
    }
}
