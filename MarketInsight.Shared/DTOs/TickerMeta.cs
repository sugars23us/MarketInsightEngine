
using System;

namespace MarketInsight.Shared.DTOs
{
    /// <summary>
    /// Slowly-changing per-ticker metadata used by indicators.
    /// Pull from fundamentals vendor (float, 3M ADV, etc.) and
    /// warm the cache at worker start.
    /// </summary>
    public sealed class TickerMeta
    {
        public int StockId { get; init; }
        public string Ticker { get; init; } = string.Empty;
        public string Exchange { get; init; } = string.Empty;

        /// <summary>Free float shares (notional), used for Float Rotation.</summary>
        public long? FreeFloatShares { get; init; }

        /// <summary>Average daily volume over ~3 months (shares).</summary>
        public long? AvgDailyVolume3M { get; init; }

        /// <summary>Optional enterprise value / market cap snapshot (for filters).</summary>
        public decimal? MarketCap { get; init; }

        /// <summary>UTC timestamp of the snapshot.</summary>
        public DateTime? SnapshotUtc { get; init; }
    }
}
