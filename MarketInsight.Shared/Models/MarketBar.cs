namespace MarketInsight.Shared.Models;

/// <summary>
/// Represents an OHLCV bar (e.g. from Polygon) at a given timeframe.
/// This is a cross-layer DTO used by the ingestion pipeline, application and infrastructure.
/// </summary>
public sealed record MarketBar
{
    /// <summary>
    /// Ticker symbol, e.g. "TSLA", "AAPL".
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Bar close time in UTC.
    /// This should correspond to the <c>e</c> field in Polygon aggregate messages.
    /// </summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>
    /// Logical timeframe of the bar (1m, 1d, etc.).
    /// Maps to the TIMEFRAME table in the DB.
    /// </summary>
    public Timeframe Timeframe { get; init; }

    /// <summary>Open price.</summary>
    public decimal Open { get; init; }

    /// <summary>High price.</summary>
    public decimal High { get; init; }

    /// <summary>Low price.</summary>
    public decimal Low { get; init; }

    /// <summary>Close price.</summary>
    public decimal Close { get; init; }

    /// <summary>Total traded volume during this bar.</summary>
    public long Volume { get; init; }

    /// <summary>
    /// Volume-weighted average price for the bar, if provided by the data source.
    /// For Polygon, this is the <c>vw</c> field on aggregate messages.
    /// </summary>
    public decimal? Vwap { get; init; }

    /// Number of trades that occurred in this bar, if provided by the data source.
    public long TradeCount { get; init; }
}
