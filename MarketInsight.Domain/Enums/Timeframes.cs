namespace MarketInsight.Domain.Enums;

/// <summary>
/// Stable IDs for timeframes used across DB and code.
/// Match these to your BAR.TimeframeId values.
/// </summary>
public static class Timeframes
{
    // Intraday
    public const short Min1 = 1;
    public const short Min5 = 2;
    public const short Min15 = 3;
    public const short Min30 = 4;
    public const short Hour1 = 6;

    // Daily/above
    public const short Day = 7;
    public const short Week = 8;
    public const short Month = 9;
}
