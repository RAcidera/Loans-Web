using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanAuditLog;

public sealed record GetLoanAuditLogQuery(string LoanId) : IRequest<List<LoanAuditLogEntryDto>>;

/// <summary>Backs the Loan Details "Audit Log" tab — see LoanAuditLogEntry.</summary>
public sealed class GetLoanAuditLogQueryHandler : IRequestHandler<GetLoanAuditLogQuery, List<LoanAuditLogEntryDto>>
{
    private readonly ILoanAuditLogRepository _loanAuditLogRepository;

    public GetLoanAuditLogQueryHandler(ILoanAuditLogRepository loanAuditLogRepository)
    {
        _loanAuditLogRepository = loanAuditLogRepository;
    }

    public async Task<List<LoanAuditLogEntryDto>> Handle(GetLoanAuditLogQuery request, CancellationToken ct)
    {
        var entries = await _loanAuditLogRepository.GetByLoanIdAsync(LoanId.Parse(request.LoanId), ct);
        return entries.OrderByDescending(e => e.OccurredAtUtc).Select(e => e.ToDto()).ToList();
    }
}
