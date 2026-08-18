using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Promises.Queries.GetPromiseAuditLog;

public sealed record GetPromiseAuditLogQuery(string PromiseId) : IRequest<List<PromiseAuditLogEntryDto>>;

public sealed class GetPromiseAuditLogQueryHandler : IRequestHandler<GetPromiseAuditLogQuery, List<PromiseAuditLogEntryDto>>
{
    private readonly IPromiseAuditLogRepository _promiseAuditLogRepository;

    public GetPromiseAuditLogQueryHandler(IPromiseAuditLogRepository promiseAuditLogRepository)
    {
        _promiseAuditLogRepository = promiseAuditLogRepository;
    }

    public async Task<List<PromiseAuditLogEntryDto>> Handle(GetPromiseAuditLogQuery request, CancellationToken ct)
    {
        var entries = await _promiseAuditLogRepository.GetByPromiseIdAsync(PromiseToPayId.Parse(request.PromiseId), ct);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
