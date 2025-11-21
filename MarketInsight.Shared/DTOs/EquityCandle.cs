namespace MarketInsight.Application.Engine;

public readonly record struct EquityCandle(
    int EquityId,
    byte TimeframeId,
    DateTime TsUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal? Vwap,      // session VWAP from "a"
    long? Ats           // average trade size from "z"
);