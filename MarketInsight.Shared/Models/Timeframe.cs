namespace MarketInsight.Shared.Models;

/// <summary>
/// Logical bar timeframes used across the system. Stored as byte in the DB.
/// </summary>
public enum Timeframe : byte
{
    /// <summary>1-minute bars (e.g. Polygon AM.*)</summary>
    M1 = 1,

    /// <summary>Daily bars (end-of-day OHLCV)</summary>
    D1 = 2,
}
