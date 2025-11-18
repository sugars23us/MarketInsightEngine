using MarketInsight.Infrastructure.Persistence;
using MarketInsight.Shared.DTOs;

namespace MarketInsight.Infrastructure.Extensions
{
    public static class StockEntityExtensions
    {
        public static TickerMeta ToTickerMeta(this StockEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return new TickerMeta
            {
                StockId = entity.StockId,
                Ticker = entity.Ticker ?? string.Empty,
                Exchange = entity.Exchange.ToString() ?? string.Empty,
                FreeFloatShares = entity.FloatShares,
                AvgDailyVolume3M = entity.Adv63,
                //MarketCap = entity.MarketCap,
                //SnapshotUtc = entity.UpdatedUtc ?? DateTime.UtcNow
            };
        }
    }
}
