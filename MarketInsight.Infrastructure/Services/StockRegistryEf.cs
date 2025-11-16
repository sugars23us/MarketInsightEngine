using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Services;
using MarketInsight.Infrastructure.Persistence;
using MarketInsight.Shared.DTOs;
using MarketInsight.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketInsight.Infrastructure.Services
{
    /// <summary>
    /// Async EF Core based implementation of IStockRegistry, with in-memory cache.
    /// Uses IDbContextFactory to stay safe as a singleton.
    /// </summary>
    public sealed class StockRegistryEf : IStockRegistry
    {
        private readonly IDbContextFactory<MarketDbContext> _factory;
        private readonly ILogger<StockRegistryEf> _log;
        private readonly ConcurrentDictionary<int, TickerMeta> _cache = new();

        public StockRegistryEf(IDbContextFactory<MarketDbContext> factory,
                               ILogger<StockRegistryEf> log)
        {
            _factory = factory;
            _log = log;
        }

        public Task<TickerMeta?> GetMetaAsync(int stockId, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(stockId, out var cached))
                return Task.FromResult<TickerMeta?>(cached);

            return LoadAndCacheAsync(stockId, ct);
        }

        private async Task<TickerMeta?> LoadAndCacheAsync(int stockId, CancellationToken ct)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

                var entity = await db.Stocks
                    .AsNoTracking()
                    .SingleOrDefaultAsync(s => s.StockId == stockId, ct)
                    .ConfigureAwait(false);

                if (entity == null)
                {
                    _log.LogWarning("Stock metadata not found for StockId={StockId}", stockId);
                    return null;
                }

                var meta = new TickerMeta
                {
                    StockId = entity.StockId,
                    Ticker = entity.Ticker,
                    Exchange = entity.Exchange ?? string.Empty,
                    FreeFloatShares = entity.FloatShares,
                    AvgDailyVolume3M = entity.Adv63,
                    SnapshotUtc = DateTime.UtcNow
                };

                _cache[stockId] = meta;
                return meta;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error loading metadata for StockId={StockId}", stockId);
                return null;
            }
        }
    }
}
