using System;

namespace MarketInsight.Domain.Extensions;

/// <summary>
/// Candle/price helpers that work with raw OHLC values or tuples,
/// so Domain stays decoupled from any DTO types.
/// </summary>
public static class BarExtensions
{
    // ---------- Scalar overloads (explicit OHLC) ----------

    /// <summary>Total range = High - Low.</summary>
    public static decimal Range(decimal high, decimal low) => high - low;

    /// <summary>Real body = |Close - Open|.</summary>
    public static decimal Body(decimal open, decimal close)
        => Math.Abs(close - open);

    /// <summary>Upper wick = High - max(Open, Close), never negative.</summary>
    public static decimal UpperWick(decimal open, decimal close, decimal high)
        => Math.Max(0m, high - Math.Max(open, close));

    /// <summary>Lower wick = min(Open, Close) - Low, never negative.</summary>
    public static decimal LowerWick(decimal open, decimal close, decimal low)
        => Math.Max(0m, Math.Min(open, close) - low);

    /// <summary>True Range = max(High-Low, |High-PrevClose|, |Low-PrevClose|).</summary>
    public static decimal TrueRange(decimal high, decimal low, decimal prevClose)
    {
        var r1 = high - low;
        var r2 = Math.Abs(high - prevClose);
        var r3 = Math.Abs(low - prevClose);
        return Math.Max(r1, Math.Max(r2, r3));
    }

    /// <summary>Mid price = (High + Low)/2.</summary>
    public static decimal Mid(decimal high, decimal low) => (high + low) / 2m;

    /// <summary>Returns +1 for bullish (Close &gt; Open), −1 for bearish, 0 for doji.</summary>
    public static int Direction(decimal open, decimal close)
        => close > open ? +1 : (close < open ? -1 : 0);

    /// <summary>VWAP deviation: (Close - VWAP)/VWAP. Returns 0 if VWAP == 0.</summary>
    public static decimal VwapDeviation(decimal close, decimal vwap)
        => vwap == 0m ? 0m : (close - vwap) / vwap;

    // ---------- Tuple-friendly overloads ----------

    /// <summary>Range for a (High, Low) tuple.</summary>
    public static decimal Range(this (decimal High, decimal Low) x) => x.High - x.Low;

    /// <summary>Body for a (Open, Close) tuple.</summary>
    public static decimal Body(this (decimal Open, decimal Close) x)
        => Math.Abs(x.Close - x.Open);

    /// <summary>TrueRange for (High, Low, PrevClose) tuple.</summary>
    public static decimal TrueRange(this (decimal High, decimal Low, decimal PrevClose) x)
    {
        var r1 = x.High - x.Low;
        var r2 = Math.Abs(x.High - x.PrevClose);
        var r3 = Math.Abs(x.Low - x.PrevClose);
        return Math.Max(r1, Math.Max(r2, r3));
    }

    /// <summary>VWAP deviation for (Close, Vwap) tuple.</summary>
    public static decimal VwapDeviation(this (decimal Close, decimal Vwap) x)
        => x.Vwap == 0m ? 0m : (x.Close - x.Vwap) / x.Vwap;
}
