namespace LoanManagementSystem.Application.Common.DateTimeHandling;

public sealed class AppDateTimeService : IAppDateTimeService
{
    private readonly TimeProvider _timeProvider;
    private readonly IBusinessTimeZoneCache _timeZoneCache;

    public AppDateTimeService(TimeProvider timeProvider, IBusinessTimeZoneCache timeZoneCache)
    {
        _timeProvider = timeProvider;
        _timeZoneCache = timeZoneCache;
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public string BusinessTimeZoneId => _timeZoneCache.CurrentTimeZoneId;

    public DateOnly Today => BusinessTimeZoneCalculator.GetBusinessToday(UtcNow, BusinessTimeZoneId);

    public TimeOnly TimeOfDay => BusinessTimeZoneCalculator.GetBusinessTimeOfDay(UtcNow, BusinessTimeZoneId);

    public DateTime ConvertUtcToBusinessLocal(DateTime utcDateTime) =>
        BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal(utcDateTime, BusinessTimeZoneId);

    public DateTime ConvertBusinessLocalToUtc(DateTime businessLocalDateTime) =>
        BusinessTimeZoneCalculator.ConvertBusinessLocalToUtc(businessLocalDateTime, BusinessTimeZoneId);

    public DateTime GetStartOfBusinessDayUtc(DateOnly date) =>
        BusinessTimeZoneCalculator.GetStartOfBusinessDayUtc(date, BusinessTimeZoneId);
}
