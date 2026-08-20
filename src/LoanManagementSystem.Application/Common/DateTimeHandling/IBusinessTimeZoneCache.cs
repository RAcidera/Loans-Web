using LoanManagementSystem.Domain.Repositories;

namespace LoanManagementSystem.Application.Common.DateTimeHandling;

/// <summary>
/// Holds the currently-configured Business Time Zone id in memory so
/// IAppDateTimeService can stay fully synchronous (every "today" call site
/// is a one-line swap, no async signature churn) despite the setting living
/// in the database. Populated once at startup and re-populated immediately
/// by UpdateBusinessTimeZoneCommandHandler after a save, so a change takes
/// effect for the very next request rather than after some cache TTL.
/// </summary>
public interface IBusinessTimeZoneCache
{
    string CurrentTimeZoneId { get; }

    Task RefreshAsync(ISettingsRepository settingsRepository, CancellationToken ct = default);
}
