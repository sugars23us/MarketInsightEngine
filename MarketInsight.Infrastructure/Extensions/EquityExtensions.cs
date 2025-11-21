using MarketInsight.Infrastructure.Entities;
using MarketInsight.Shared.DTOs;

public static class EquityExtensions
{
    public static Equity ToEquity(this EquityEntity entity)
    {
        return new Equity
        {
            EquityId = entity.EquityId,
            Ticker = entity.Ticker,
            Name = entity.Name,
            Exchange = entity.Exchange,
            FloatShares = entity.FloatShares,
            AvgVolume3M = entity.AvgVolume3M,
            MarketCap = entity.MarketCap,
            UpdatedUtc = entity.UpdatedUtc
        };
    }
}