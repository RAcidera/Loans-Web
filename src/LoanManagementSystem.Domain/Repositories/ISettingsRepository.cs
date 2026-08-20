using LoanManagementSystem.Domain.Settings;

namespace LoanManagementSystem.Domain.Repositories;

public interface ISettingsRepository
{
    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default);

    void Add(AppSetting setting);
}
