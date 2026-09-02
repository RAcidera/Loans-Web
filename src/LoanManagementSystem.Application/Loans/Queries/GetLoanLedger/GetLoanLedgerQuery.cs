using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanLedger;

public sealed record GetLoanLedgerQuery(string LoanId) : IRequest<List<LoanLedgerEntryDto>>;

/// <summary>Backs the Loan Details ledger view and the Payments/Extensions tabs' Running Balance columns — see LoanLedgerEntry.</summary>
public sealed class GetLoanLedgerQueryHandler : IRequestHandler<GetLoanLedgerQuery, List<LoanLedgerEntryDto>>
{
    private readonly ILoanLedgerRepository _loanLedgerRepository;

    public GetLoanLedgerQueryHandler(ILoanLedgerRepository loanLedgerRepository)
    {
        _loanLedgerRepository = loanLedgerRepository;
    }

    public async Task<List<LoanLedgerEntryDto>> Handle(GetLoanLedgerQuery request, CancellationToken ct)
    {
        var entries = await _loanLedgerRepository.GetByLoanIdAsync(LoanId.Parse(request.LoanId), ct);
        var ordered = entries.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc).ToList();

        // Recomputed chronologically from SignedAmount rather than trusting
        // each row's stamped RunningBalance — see LoanLedgerEntry's doc
        // comment for why the stamped value alone can't be trusted once a
        // payment/extension has been antedated or edited to an earlier date.
        var runningBalance = 0m;
        var balanceByEntryId = new Dictionary<LoanLedgerEntryId, decimal>();
        foreach (var entry in ordered)
        {
            runningBalance += entry.SignedAmount;
            balanceByEntryId[entry.Id] = runningBalance;
        }

        return ordered.Select(e => e.ToDto(balanceByEntryId[e.Id])).ToList();
    }
}
