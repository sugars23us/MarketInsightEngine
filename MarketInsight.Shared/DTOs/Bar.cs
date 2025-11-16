
using System;

namespace MarketInsight.Shared.DTOs
{
    /// <summary>
    /// Normalized OHLCV bar coming from the ingestion layer.
    /// Times are UTC. TimeframeId matches your TIMEFRAME table
    /// (e.g. 1 = 1-minute, 2 = 5-minute, etc.).
    /// </summary>
    public sealed class Bar
    {
        public int StockId { get; init; }
        public byte TimeframeId { get; init; }
        public DateTime TsUtc { get; init; }

        public decimal Open  { get; init; }
        public decimal High  { get; init; }
        public decimal Low   { get; init; }
        public decimal Close { get; init; }

        /// <summary>Total shares traded in the bar.</summary>
        public long Volume { get; init; }

        /// <summary>Volume-Weighted Average Price for the bar (if available).</summary>
        public decimal? Vwap { get; init; }

        /// <summary>Number of reported trades in the bar.</summary>
        public int? TradeCount { get; init; }
    }
}
