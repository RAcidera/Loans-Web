using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Interest;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.GetInterestEarnedLoanBreakdown;

/// <summary>The Loan Interest Drill-Down (spec §18) — every earning period for one loan (original + each extension), for the "how did the system arrive at this number" dialog on the report grid.</summary>
public sealed record GetInterestEarnedLoanBreakdownQuery(string LoanId, DateOnly FromDate, DateOnly ToDate) : IRequest<InterestEarnedLoanBreakdownDto>;

public sealed class GetInterestEarnedLoanBreakdownQueryHandler : IRequestHandler<GetInterestEarnedLoanBreakdownQuery, InterestEarnedLoanBreakdownDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInterestCalculationService _interestCalculationService;

    public GetInterestEarnedLoanBreakdownQueryHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository, IInterestCalculationService interestCalculationService)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _interestCalculationService = interestCalculationService;
    }

    public async Task<InterestEarnedLoanBreakdownDto> Handle(GetInterestEarnedLoanBreakdownQuery request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);

        // GetAllWithDetailsAsync guarantees Extensions are loaded — the plain
        // GetByIdAsync doesn't eager-load them, and this breakdown needs every
        // extension's own period.
        var loans = await _loanRepository.GetAllWithDetailsAsync(ct);
        var loan = loans.FirstOrDefault(l => l.Id == loanId)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var names = await _customerRepository.GetNamesByIdsAsync(new[] { loan.CustomerId }, ct);
        var customerName = names.TryGetValue(loan.CustomerId, out var name) ? name : "Unknown";

        var breakdown = _interestCalculationService.Calculate(loan, request.FromDate, request.ToDate);

        var periods = breakdown.Periods.Select(p => new InterestEarnedPeriodDto(
            Label: p.Label,
            PeriodStart: p.PeriodStart.ToString("yyyy-MM-dd"),
            PeriodEndInclusive: p.PeriodEndInclusive.ToString("yyyy-MM-dd"),
            TermDays: p.TermDays,
            ContractAmount: p.ContractAmount,
            DailyInterest: p.DailyInterest,
            EarnedDaysThisPeriod: p.EarnedDaysThisPeriod,
            EarnedBeforePeriod: p.EarnedBeforePeriod,
            EarnedThisPeriod: p.EarnedThisPeriod,
            TotalEarned: p.TotalEarnedThroughToDate
        )).ToList();

        return new InterestEarnedLoanBreakdownDto(
            LoanId: loan.Id.ToString(),
            LoanNumber: MappingExtensions.FormatLoanNumber(loan.LoanNumber),
            CustomerName: customerName,
            OriginalContractInterest: breakdown.OriginalContractInterest,
            AdjustedContractInterest: breakdown.AdjustedContractInterest,
            InterestAdjustment: breakdown.InterestAdjustment,
            Periods: periods
        );
    }
}
