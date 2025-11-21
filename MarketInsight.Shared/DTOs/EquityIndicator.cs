namespace MarketInsight.Application.Engine;

public readonly record struct EquityIndicator(
    int EquityId,
    byte TimeframeId,
    DateTime TsUtc,
    string MetricCode,
    short Period,
    decimal Value,
    string? ParamsJson = null
);