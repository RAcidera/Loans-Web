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
        return entries.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc).Select(e => e.ToDto()).ToList();
    }
}
