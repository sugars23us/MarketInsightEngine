using System;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Represents a computed indicator value aligned to a specific bar.
    /// </summary>
    public readonly record struct IndicatorValue(
        int StockId,
        byte TimeframeId,
        DateTime TsUtc,
        string MetricCode,
        short Period,
        decimal Value,
        string? ParamsJson = null
    );
}
