namespace MarketInsight.Ingestor.Data;

public sealed record IndicatorRecord(
    int StockId,
    byte TimeframeId,
    DateTime TsUtc,
    string MetricCode,
    short? Period,
    decimal Value,
    string? ParamsJson = null);
