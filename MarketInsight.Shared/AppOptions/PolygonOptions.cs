namespace MarketInsight.Shared.Options;

/// <summary>
/// Strongly-typed configuration for Polygon data access.
/// Bind from <c>"Polygon"</c> section in appsettings.json.
/// </summary>
public sealed class PolygonOptions
{
    public const string SectionName = "Polygon";

    /// <summary>
    /// WebSocket URL for stock aggregates.
    /// Typically "wss://delayed.polygon.io/stocks" or "wss://socket.polygon.io/stocks".
    /// </summary>
    public string StocksWsUrl { get; set; } = "wss://delayed.polygon.io/stocks";

    /// <summary>
    /// Optional WebSocket URL for options streams (if/when you add options flow).
    /// E.g. "wss://delayed.polygon.io/options".
    /// </summary>
    public string? OptionsWsUrl { get; set; }

    /// <summary>
    /// Polygon API key used for both REST and WebSocket authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of tickers to subscribe to in the WebSocket.
    /// Example: "TSLA,MSFT,AAPL".
    /// </summary>
    public string Symbols { get; set; } = string.Empty;

    /// <summary>
    /// Optional REST base URL. Defaults to Polygon public base if empty.
    /// E.g. "https://api.polygon.io".
    /// </summary>
    public string? RestBaseUrl { get; set; }
}
