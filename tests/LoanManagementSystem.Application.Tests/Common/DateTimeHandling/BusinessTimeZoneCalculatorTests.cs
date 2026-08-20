using LoanManagementSystem.Application.Common.DateTimeHandling;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Common.DateTimeHandling;

public class BusinessTimeZoneCalculatorTests
{
    private const string Manila = "Asia/Manila";
    private const string NewYork = "America/New_York";

    [Fact]
    public void GetBusinessToday_UtcEveningInManila_IsAlreadyTheNextDayLocally()
    {
        // The exact scenario the Business Time Zone feature exists to fix:
        // 2026-08-19 17:00 UTC is 2026-08-20 01:00 in Manila (UTC+8) — "today"
        // must read Aug 20, not Aug 19 (what DateOnly.FromDateTime(DateTime.UtcNow) used to return).
        var utcNow = new DateTime(2026, 8, 19, 17, 0, 0, DateTimeKind.Utc);

        var today = BusinessTimeZoneCalculator.GetBusinessToday(utcNow, Manila);

        Assert.Equal(new DateOnly(2026, 8, 20), today);
    }

    [Fact]
    public void GetBusinessToday_UtcEarlyMorningInManila_IsStillYesterdayUtc()
    {
        // 2026-08-20 03:00 UTC is 2026-08-20 11:00 Manila — same calendar day
        // in both, but 2026-08-19 20:00 UTC is 2026-08-20 04:00 Manila: the
        // UTC date (19th) already lags the Manila date (20th) at that instant.
        var utcNow = new DateTime(2026, 8, 19, 20, 0, 0, DateTimeKind.Utc);

        var today = BusinessTimeZoneCalculator.GetBusinessToday(utcNow, Manila);

        Assert.Equal(new DateOnly(2026, 8, 20), today);
    }

    [Fact]
    public void ConvertUtcToBusinessLocal_Manila_AddsEightHours()
    {
        var utc = new DateTime(2026, 8, 20, 15, 30, 0, DateTimeKind.Utc);

        var local = BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal(utc, Manila);

        Assert.Equal(new DateTime(2026, 8, 20, 23, 30, 0), local);
    }

    [Fact]
    public void ConvertBusinessLocalToUtc_Manila_SubtractsEightHours()
    {
        var local = new DateTime(2026, 8, 20, 0, 0, 0);

        var utc = BusinessTimeZoneCalculator.ConvertBusinessLocalToUtc(local, Manila);

        Assert.Equal(new DateTime(2026, 8, 19, 16, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void GetStartOfBusinessDayUtc_Manila_IsMidnightManilaExpressedInUtc()
    {
        var startUtc = BusinessTimeZoneCalculator.GetStartOfBusinessDayUtc(new DateOnly(2026, 8, 20), Manila);

        Assert.Equal(new DateTime(2026, 8, 19, 16, 0, 0, DateTimeKind.Utc), startUtc);
    }

    [Fact]
    public void GetStartOfBusinessDayUtc_HalfOpenRange_CorrectlyBoundsAWholeManilaBusinessDay()
    {
        var startOfAug20 = BusinessTimeZoneCalculator.GetStartOfBusinessDayUtc(new DateOnly(2026, 8, 20), Manila);
        var startOfAug21 = BusinessTimeZoneCalculator.GetStartOfBusinessDayUtc(new DateOnly(2026, 8, 21), Manila);

        // A payment recorded at 11:59 PM Manila time on Aug 20 (still Aug 20
        // business-locally) must fall inside [startOfAug20, startOfAug21).
        var lateEveningAug20Manila = new DateTime(2026, 8, 20, 23, 59, 0);
        var instantUtc = BusinessTimeZoneCalculator.ConvertBusinessLocalToUtc(lateEveningAug20Manila, Manila);

        Assert.True(instantUtc >= startOfAug20 && instantUtc < startOfAug21);
    }

    [Fact]
    public void GetBusinessToday_MonthEndRollover_CrossesIntoNextMonth()
    {
        // 2026-08-31 20:00 UTC = 2026-09-01 04:00 Manila.
        var utcNow = new DateTime(2026, 8, 31, 20, 0, 0, DateTimeKind.Utc);

        var today = BusinessTimeZoneCalculator.GetBusinessToday(utcNow, Manila);

        Assert.Equal(new DateOnly(2026, 9, 1), today);
    }

    [Fact]
    public void GetBusinessToday_YearEndRollover_CrossesIntoNextYear()
    {
        // 2026-12-31 20:00 UTC = 2027-01-01 04:00 Manila.
        var utcNow = new DateTime(2026, 12, 31, 20, 0, 0, DateTimeKind.Utc);

        var today = BusinessTimeZoneCalculator.GetBusinessToday(utcNow, Manila);

        Assert.Equal(new DateOnly(2027, 1, 1), today);
    }

    [Fact]
    public void ConvertUtcToBusinessLocal_NewYork_UsesDaylightOffsetInSummer()
    {
        // America/New_York is UTC-4 during EDT (daylight saving, roughly
        // March-November) and UTC-5 during EST otherwise — unlike Manila,
        // a fixed-offset conversion would be wrong here for half the year.
        var summerUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        var local = BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal(summerUtc, NewYork);

        Assert.Equal(new DateTime(2026, 7, 15, 8, 0, 0), local); // UTC-4 (EDT)
    }

    [Fact]
    public void ConvertUtcToBusinessLocal_NewYork_UsesStandardOffsetInWinter()
    {
        var winterUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var local = BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal(winterUtc, NewYork);

        Assert.Equal(new DateTime(2026, 1, 15, 7, 0, 0), local); // UTC-5 (EST)
    }

    [Fact]
    public void GetBusinessToday_NewYork_SpringForwardTransition_StillResolvesCorrectDay()
    {
        // DST begins 2026-03-08 in the US (2:00 AM -> 3:00 AM EST->EDT). A
        // business day's midnight boundary is unaffected by a 2 AM
        // transition, but this confirms GetBusinessToday doesn't throw or
        // misresolve on a DST-transition day.
        var justAfterMidnightUtc = new DateTime(2026, 3, 8, 5, 30, 0, DateTimeKind.Utc); // 12:30 AM EST

        var today = BusinessTimeZoneCalculator.GetBusinessToday(justAfterMidnightUtc, NewYork);

        Assert.Equal(new DateOnly(2026, 3, 8), today);
    }

    [Fact]
    public void GetBusinessToday_NewYork_FallBackTransition_StillResolvesCorrectDay()
    {
        // DST ends 2026-11-01 in the US (2:00 AM EDT -> 1:00 AM EST).
        var lateEveningUtc = new DateTime(2026, 11, 1, 4, 30, 0, DateTimeKind.Utc); // 12:30 AM EDT (before fallback)

        var today = BusinessTimeZoneCalculator.GetBusinessToday(lateEveningUtc, NewYork);

        Assert.Equal(new DateOnly(2026, 11, 1), today);
    }

    [Fact]
    public void ResolveTimeZone_UnknownId_Throws()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => BusinessTimeZoneCalculator.ResolveTimeZone("Not/A_Real_Zone"));
    }
}
