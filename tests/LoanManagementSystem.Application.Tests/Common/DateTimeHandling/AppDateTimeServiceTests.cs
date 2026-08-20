using LoanManagementSystem.Application.Common.DateTimeHandling;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Common.DateTimeHandling;

/// <summary>
/// Confirms IAppDateTimeService can be fully frozen for tests (no dependency
/// on the machine clock, per the feature's testability requirement) by
/// swapping in a fixed TimeProvider and a fixed IBusinessTimeZoneCache.
/// </summary>
public class AppDateTimeServiceTests
{
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class FixedTimeZoneCache : IBusinessTimeZoneCache
    {
        public string CurrentTimeZoneId { get; set; } = "Asia/Manila";
        public Task RefreshAsync(Domain.Repositories.ISettingsRepository settingsRepository, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    [Fact]
    public void Today_UsesFrozenClockAndConfiguredTimeZone_NotTheMachineClock()
    {
        // 2026-08-19 17:00 UTC = 2026-08-20 01:00 Manila.
        var frozenUtc = new DateTimeOffset(2026, 8, 19, 17, 0, 0, TimeSpan.Zero);
        var service = new AppDateTimeService(new FixedTimeProvider(frozenUtc), new FixedTimeZoneCache());

        Assert.Equal(new DateOnly(2026, 8, 20), service.Today);
    }

    [Fact]
    public void TimeOfDay_ReflectsBusinessLocalWallClock()
    {
        var frozenUtc = new DateTimeOffset(2026, 8, 19, 17, 15, 0, TimeSpan.Zero); // 01:15 AM Manila
        var service = new AppDateTimeService(new FixedTimeProvider(frozenUtc), new FixedTimeZoneCache());

        Assert.Equal(new TimeOnly(1, 15), service.TimeOfDay);
    }

    [Fact]
    public void UtcNow_ReturnsTheFrozenInstantUnchanged()
    {
        var frozenUtc = new DateTimeOffset(2026, 8, 19, 17, 0, 0, TimeSpan.Zero);
        var service = new AppDateTimeService(new FixedTimeProvider(frozenUtc), new FixedTimeZoneCache());

        Assert.Equal(frozenUtc.UtcDateTime, service.UtcNow);
    }

    [Fact]
    public void ChangingTheCachedTimeZone_ChangesTodayOnTheNextRead()
    {
        var frozenUtc = new DateTimeOffset(2026, 8, 19, 17, 0, 0, TimeSpan.Zero); // 01:00 Manila / 12:00 (noon) prior-day New York (EDT, UTC-4 in August)
        var cache = new FixedTimeZoneCache { CurrentTimeZoneId = "Asia/Manila" };
        var service = new AppDateTimeService(new FixedTimeProvider(frozenUtc), cache);

        Assert.Equal(new DateOnly(2026, 8, 20), service.Today);

        cache.CurrentTimeZoneId = "America/New_York";

        Assert.Equal(new DateOnly(2026, 8, 19), service.Today);
    }

    [Fact]
    public void ConvertUtcToBusinessLocal_AndBack_RoundTrips()
    {
        var service = new AppDateTimeService(new FixedTimeProvider(DateTimeOffset.UtcNow), new FixedTimeZoneCache());
        var utc = new DateTime(2026, 8, 20, 15, 30, 0, DateTimeKind.Utc);

        var local = service.ConvertUtcToBusinessLocal(utc);
        var backToUtc = service.ConvertBusinessLocalToUtc(local);

        Assert.Equal(utc, backToUtc);
    }
}
