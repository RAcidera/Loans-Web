namespace LoanManagementSystem.Application.Common.DateTimeHandling;

/// <summary>
/// The only place TimeZoneInfo conversion math happens for the app's
/// Business Time Zone strategy — pure functions, no I/O, no ambient clock,
/// so every case (Manila/UTC+8 boundary crossings, DST transitions, month-
/// and year-end rollovers) is directly unit-testable without a fake service
/// or a database. .NET 8's TimeZoneInfo resolves IANA ids (e.g. "Asia/Manila")
/// on both Windows and Linux without any extra package.
/// </summary>
public static class BusinessTimeZoneCalculator
{
    /// <summary>Resolves an IANA (or Windows) timezone id, throwing the same exceptions callers should translate into a validation error.</summary>
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId) => TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    /// <summary>The business-local calendar date for a given UTC instant — the single source of "today" this whole feature exists to fix.</summary>
    public static DateOnly GetBusinessToday(DateTime utcNow, string timeZoneId) =>
        DateOnly.FromDateTime(ConvertUtcToBusinessLocal(utcNow, timeZoneId));

    /// <summary>The business-local wall-clock time-of-day for a given UTC instant (Diary's Entry Time/Reminder Time defaults).</summary>
    public static TimeOnly GetBusinessTimeOfDay(DateTime utcNow, string timeZoneId) =>
        TimeOnly.FromDateTime(ConvertUtcToBusinessLocal(utcNow, timeZoneId));

    /// <summary>Converts a UTC instant to the equivalent business-local wall-clock DateTime (Kind=Unspecified — it's a local wall-clock reading, not another instant), for display.</summary>
    public static DateTime ConvertUtcToBusinessLocal(DateTime utcDateTime, string timeZoneId)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, ResolveTimeZone(timeZoneId));
    }

    /// <summary>Converts a business-local wall-clock DateTime back to the equivalent UTC instant — e.g. turning "business midnight of date X" into the UTC instant that range filters should compare against.</summary>
    public static DateTime ConvertBusinessLocalToUtc(DateTime businessLocalDateTime, string timeZoneId)
    {
        var local = DateTime.SpecifyKind(businessLocalDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, ResolveTimeZone(timeZoneId));
    }

    /// <summary>The UTC instant of business-local midnight for <paramref name="date"/> — the correct lower bound for a half-open "this business day" range filter against a UTC timestamp column (use GetStartOfBusinessDayUtc(date.AddDays(1)) as the exclusive upper bound).</summary>
    public static DateTime GetStartOfBusinessDayUtc(DateOnly date, string timeZoneId) =>
        ConvertBusinessLocalToUtc(date.ToDateTime(TimeOnly.MinValue), timeZoneId);
}
