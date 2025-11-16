using System;

namespace MarketInsight.Domain.Extensions;

/// <summary>
/// Time helpers with DST-safe conversion for New York session logic.
/// Works on Windows ("Eastern Standard Time") and Linux ("America/New_York").
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Gets a TimeZoneInfo for New York, trying Windows and IANA IDs.
    /// </summary>
    public static TimeZoneInfo GetNewYorkTz()
    {
        // Windows
        try { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
        catch { /* ignored */ }

        // Linux / macOS
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch { /* ignored */ }

        throw new TimeZoneNotFoundException(
            "Could not resolve New York time zone ('Eastern Standard Time' or 'America/New_York').");
    }

    /// <summary>
    /// Converts a UTC DateTime to New York local time. Input must be UTC.
    /// </summary>
    public static DateTime ToNewYork(this DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, GetNewYorkTz());
    }

    /// <summary>
    /// Returns the New York local DATE for a UTC timestamp (used as TradingDate).
    /// </summary>
    public static DateTime GetNyTradingDate(this DateTime utc)
    {
        var ny = utc.ToNewYork();
        return ny.Date;
    }

    /// <summary>
    /// True if the UTC time falls inside the regular US session (09:30–16:00 New York), Mon–Fri.
    /// </summary>
    public static bool IsRegularUsSession(this DateTime utc)
    {
        var ny = utc.ToNewYork();
        var dow = ny.DayOfWeek;
        if (dow is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;

        var t = ny.TimeOfDay;
        return t >= TimeSpan.FromHours(9.5) && t < TimeSpan.FromHours(16);
    }

    /// <summary>Truncates to whole seconds (useful to align with datetime2(0)).</summary>
    public static DateTime TruncateToSecond(this DateTime dt)
        => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);

    /// <summary>Truncates to whole minutes.</summary>
    public static DateTime TruncateToMinute(this DateTime dt)
        => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);

    /// <summary>
    /// Returns the next regular-session open in New York after the given UTC time.
    /// Does not account for market holidays.
    /// </summary>
    public static DateTime NextUsSessionOpenUtc(this DateTime utc)
    {
        var nyTz = GetNewYorkTz();
        if (utc.Kind != DateTimeKind.Utc) utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        DateTime ny = utc.ToNewYork().Date; // 00:00 local
        while (true)
        {
            // If weekend, advance to Monday
            if (ny.DayOfWeek is DayOfWeek.Saturday) ny = ny.AddDays(2);
            else if (ny.DayOfWeek is DayOfWeek.Sunday) ny = ny.AddDays(1);

            var openNy = ny.AddHours(9).AddMinutes(30);
            var openUtc = TimeZoneInfo.ConvertTimeToUtc(openNy, nyTz);
            if (openUtc > utc) return openUtc;

            ny = ny.AddDays(1);
        }
    }
}
