using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.GetCashLedger;

public sealed record GetCashLedgerQuery : IRequest<List<CashLedgerEntryDto>>;

public sealed class GetCashLedgerQueryHandler : IRequestHandler<GetCashLedgerQuery, List<CashLedgerEntryDto>>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;

    public GetCashLedgerQueryHandler(ICashLedgerRepository cashLedgerRepository)
    {
        _cashLedgerRepository = cashLedgerRepository;
    }

    public async Task<List<CashLedgerEntryDto>> Handle(GetCashLedgerQuery request, CancellationToken ct)
    {
        var entries = await _cashLedgerRepository.GetAllAsync(ct);
        return entries
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Select(e => e.ToDto())
            .ToList();
    }
}
