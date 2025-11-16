using System;

namespace MarketInsight.Domain.Extensions;

/// <summary>
/// Numeric helpers for safe math on decimals.
/// </summary>
public static class DecimalExtensions
{
    /// <summary>Safe division: returns 0 when denominator == 0.</summary>
    public static decimal SafeDiv(this decimal numerator, decimal denominator)
        => denominator == 0m ? 0m : numerator / denominator;

    /// <summary>Clamps a value between min and max.</summary>
    public static decimal Clamp(this decimal value, decimal min, decimal max)
        => value < min ? min : (value > max ? max : value);

    /// <summary>Returns true if |value| &lt;= eps.</summary>
    public static bool IsZero(this decimal value, decimal eps = 0.00000001m)
        => Math.Abs(value) <= eps;

    /// <summary>Percent change: (to/from) - 1.0m. Returns 0 if from == 0.</summary>
    public static decimal PercentChange(this decimal from, decimal to)
        => from == 0m ? 0m : (to / from) - 1m;

    /// <summary>Rounds to given decimals using MidpointAwayFromZero.</summary>
    public static decimal RoundTo(this decimal value, int decimals)
        => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
