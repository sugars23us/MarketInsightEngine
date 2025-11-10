using System.Text.Json.Serialization;

namespace MarketInsight.Ingestor.Ingestion;

// Sample AM payload fields (important subset)
public sealed class PolygonAggregateMessage
{
    [JsonPropertyName("ev")] public string? Event { get; set; }     // "AM"
    [JsonPropertyName("sym")] public string? Symbol { get; set; }   // "AAPL"
    [JsonPropertyName("o")] public decimal Open { get; set; }
    [JsonPropertyName("h")] public decimal High { get; set; }
    [JsonPropertyName("l")] public decimal Low { get; set; }
    [JsonPropertyName("c")] public decimal Close { get; set; }
    [JsonPropertyName("v")] public long Volume { get; set; }
    [JsonPropertyName("vw")] public decimal? Vwap { get; set; }
    [JsonPropertyName("s")] public long StartEpochMs { get; set; }  // bar start
    [JsonPropertyName("e")] public long EndEpochMs { get; set; }    // bar end (use as TsUtc)
    [JsonPropertyName("z")] public int? TradeCount { get; set; }    // optional
}
