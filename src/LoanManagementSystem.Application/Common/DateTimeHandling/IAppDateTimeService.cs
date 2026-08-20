namespace LoanManagementSystem.Application.Common.DateTimeHandling;

/// <summary>
/// The authoritative date/time provider for the application — every
/// handler that used to call DateOnly.FromDateTime(DateTime.UtcNow) (UTC
/// midnight, not the business's actual local midnight) injects this and
/// uses Today instead. Fully synchronous and backed by TimeProvider (so
/// tests can freeze the clock) + IBusinessTimeZoneCache (so it never blocks
/// on I/O) — see those two types for how "the current instant" and "the
/// current business timezone" are actually sourced.
/// </summary>
public interface IAppDateTimeService
{
    /// <summary>The current instant, UTC. Prefer this over DateTime.UtcNow directly so tests can freeze it.</summary>
    DateTime UtcNow { get; }

    /// <summary>Today's date in the configured Business Time Zone — the correct anchor for every "today"/"this month" business computation (overdue status, dashboard KPIs, diary/calendar "today").</summary>
    DateOnly Today { get; }

    /// <summary>The current wall-clock time of day in the configured Business Time Zone (Diary's Entry Time/Reminder Time defaults).</summary>
    TimeOnly TimeOfDay { get; }

    /// <summary>The configured Business Time Zone's IANA id (e.g. "Asia/Manila").</summary>
    string BusinessTimeZoneId { get; }

    /// <summary>Converts a UTC instant to the equivalent business-local wall-clock time, for display.</summary>
    DateTime ConvertUtcToBusinessLocal(DateTime utcDateTime);

    /// <summary>Converts a business-local wall-clock time back to the equivalent UTC instant.</summary>
    DateTime ConvertBusinessLocalToUtc(DateTime businessLocalDateTime);

    /// <summary>The UTC instant of business-local midnight for <paramref name="date"/> — the correct lower bound of a half-open "this business day" range filter against a UTC timestamp column. Call with date.AddDays(1) to get the matching exclusive upper bound.</summary>
    DateTime GetStartOfBusinessDayUtc(DateOnly date);
}
