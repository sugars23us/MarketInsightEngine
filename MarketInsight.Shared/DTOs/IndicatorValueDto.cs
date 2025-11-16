
using System;

namespace MarketInsight.Shared.DTOs
{
    /// <summary>
    /// Row-shaped DTO mirroring dbo.INDICATOR_VALUE shape.
    /// Used for TVPs/bulk copy and inter-layer passing.
    /// </summary>
    public sealed class IndicatorValueDto
    {
        public int StockId { get; init; }
        public byte TimeframeId { get; init; }
        public DateTime TsUtc { get; init; }

        /// <summary>Metric identifier, e.g. "ATS_MA_15".</summary>
        public string MetricCode { get; init; } = string.Empty;

        /// <summary>Optional period for the metric (0 for instantaneous).</summary>
        public short Period { get; init; }

        /// <summary>Optional JSON-encoded parameters (sparse).</summary>
        public string? ParamsJson { get; init; }

        public decimal Value { get; init; }

        public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
    }
}
