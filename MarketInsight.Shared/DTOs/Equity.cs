namespace MarketInsight.Shared.DTOs;

public sealed class Equity
{
    public int EquityId { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Exchange { get; init; }
    public long? FloatShares { get; init; }
    public long? AvgVolume3M { get; init; }
    public decimal? MarketCap { get; init; }
    public DateTime? UpdatedUtc { get; init; }
}