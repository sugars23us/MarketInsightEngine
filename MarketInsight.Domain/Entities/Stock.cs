namespace MarketInsight.Domain.Entities;

/// <summary>
/// Static / slow-changing facts about a listed stock (one row per ticker).
/// Mirrors dbo.STOCK.
/// </summary>
public sealed class Stock
{
    /// <summary>Database identity (PK).</summary>
    public int StockId { get; set; }

    /// <summary>Ticker symbol (e.g., TSLA). Uniqueness enforced in DB.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Optional human-readable name (e.g., Tesla, Inc.).</summary>
    public string? Name { get; set; }

    /// <summary>Optional exchange code (e.g., NASDAQ, NYSE).</summary>
    public string? Exchange { get; set; }

    /// <summary>Free float shares (not total outstanding). Used for rotations, TDR, efficiency.</summary>
    public long? FloatShares { get; set; }

    /// <summary>Approx. 3-month average daily volume in shares (e.g., 63 sessions).</summary>
    public long? Adv63 { get; set; }

    /// <summary>Optional 1-month ADV (e.g., 20 sessions).</summary>
    public long? Adv20 { get; set; }

    /// <summary>UTC audit columns (optional in DB but useful for ops).</summary>
    public DateTime? InsertedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    /// <summary>Convenience helper to check if we have enough fundamentals for float-based indicators.</summary>
    public bool HasFloatBasics() => (FloatShares ?? 0) > 0;

    /// <summary>Returns a normalized, uppercase ticker (safe default).</summary>
    public string NormalizedTicker() => Ticker?.Trim().ToUpperInvariant() ?? string.Empty;

    public override string ToString() => $"{StockId}:{Ticker}";
}
