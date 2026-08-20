using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.Settings;

namespace LoanManagementSystem.Application.Common.DateTimeHandling;

/// <summary>
/// Singleton — one in-memory copy shared by every request. Falls back to
/// the product default ("Asia/Manila") only if asked before the very first
/// RefreshAsync call, which startup ordering (Program.cs seeds the default
/// AppSetting row, then immediately calls RefreshAsync before the app
/// starts serving requests) prevents in practice.
/// </summary>
public sealed class BusinessTimeZoneCache : IBusinessTimeZoneCache
{
    public const string DefaultTimeZoneId = "Asia/Manila";

    private volatile string _currentTimeZoneId = DefaultTimeZoneId;

    public string CurrentTimeZoneId => _currentTimeZoneId;

    public async Task RefreshAsync(ISettingsRepository settingsRepository, CancellationToken ct = default)
    {
        var setting = await settingsRepository.GetByKeyAsync(AppSetting.Keys.BusinessTimeZone, ct);
        if (setting is not null)
            _currentTimeZoneId = setting.Value;
    }
}
