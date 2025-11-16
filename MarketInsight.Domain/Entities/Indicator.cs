namespace MarketInsight.Domain.Entities;

/// <summary>
/// One computed indicator value at a specific timestamp and timeframe for a stock.
/// Mirrors dbo.INDICATOR_VALUE with (StockId, TimeframeId, TsUtc, MetricCode, Period) as the natural key.
/// </summary>
public sealed class Indicator : IEquatable<Indicator>
{
    /// <summary>FK to dbo.STOCK.</summary>
    public int StockId { get; set; }

    /// <summary>Timeframe id (e.g., 1 = 1-minute, 7 = daily).</summary>
    public short TimeframeId { get; set; }

    /// <summary>Bar-aligned timestamp in UTC (datetime2(0) in DB).</summary>
    public DateTime TsUtc { get; set; }

    /// <summary>Metric code (e.g., "ATS", "ATS_MA_15", "RVOL_63", "IFI_60").</summary>
    public string MetricCode { get; set; } = string.Empty;

    /// <summary>Metric period (e.g., 0 for instantaneous, 15, 60, 63, 2, 5, etc.).</summary>
    public short Period { get; set; }

    /// <summary>Optional JSON blob for parameters (kept null for most metrics).</summary>
    public string? ParamsJson { get; set; }

    /// <summary>Indicator value (DECIMAL(19,8) in DB).</summary>
    public decimal Value { get; set; }

    /// <summary>UTC audit (set by DB on upsert).</summary>
    public DateTime? UpdatedUtc { get; set; }

    /// <summary>Composite key comparison helper (matches DB unique constraint).</summary>
    public bool KeyEquals(Indicator other) =>
        other is not null &&
        StockId == other.StockId &&
        TimeframeId == other.TimeframeId &&
        TsUtc == other.TsUtc &&
        string.Equals(MetricCode, other.MetricCode, StringComparison.Ordinal) &&
        Period == other.Period;

    public bool Equals(Indicator? other) => other is not null && KeyEquals(other);

    public override bool Equals(object? obj) => obj is Indicator o && Equals(o);

    public override int GetHashCode()
    {
        unchecked
        {
            var h = 17;
            h = h * 31 + StockId;
            h = h * 31 + TimeframeId;
            h = h * 31 + TsUtc.GetHashCode();
            h = h * 31 + (MetricCode?.GetHashCode(StringComparison.Ordinal) ?? 0);
            h = h * 31 + Period.GetHashCode();
            return h;
        }
    }

    public override string ToString() =>
        $"{StockId}/{TimeframeId}@{TsUtc:O} {MetricCode}({Period})={Value}";
}
