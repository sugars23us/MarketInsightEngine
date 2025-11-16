using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Normalized bar DTO used by the engine.
    /// This is the application-layer representation; it may wrap Shared.Models.MarketBar.
    /// </summary>
    public readonly record struct MarketBar(
        int StockId,
        byte TimeframeId,
        DateTime TsUtc,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        long Volume,
        decimal? Vwap,
        long? TradeCount
    );
}
