namespace LoanManagementSystem.Application.Common.Models;

/// <summary>
/// Wire shape for server-side-paged list endpoints. PageIndex/PageSize
/// don't round-trip back — the client already knows what it asked for;
/// TotalCount is the only new information it needs to size a paginator.
/// </summary>
public sealed record PagedResult<T>(List<T> Items, int TotalCount);
