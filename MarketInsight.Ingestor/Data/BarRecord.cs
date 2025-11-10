using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Ingestor.Data
{
    public sealed record BarRecord(
    int StockId,
    byte TimeframeId,
    DateTime TsUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal? Vwap,
    int? TradeCount);
}
