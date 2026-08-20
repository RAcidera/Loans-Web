using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.Settings;
using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly AppDbContext _db;

    public SettingsRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppSetting?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        _db.Set<AppSetting>().FirstOrDefaultAsync(s => s.Key == key, ct);

    public void Add(AppSetting setting) => _db.Set<AppSetting>().Add(setting);
}
