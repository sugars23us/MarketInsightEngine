// MarketInsight.Infrastructure/Services/EquityRegistry.cs
using MarketInsight.Application.Interfaces;
using MarketInsight.Infrastructure.Entities;
using MarketInsight.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketInsight.Infrastructure.Persistence
{
    /// <summary>
    /// Async EF Core based implementation of IEquityRegistry, with in-memory cache.
    /// Uses IDbContextFactory to stay safe as a singleton.
    /// </summary>
    public sealed class EquityRegistry : IEquityRegistry
    {
        private readonly IDbContextFactory<MarketDbContext> _factory;
        private readonly ILogger<EquityRegistry> _logger;

        public EquityRegistry(
            IDbContextFactory<MarketDbContext> factory,
            ILogger<EquityRegistry> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<Equity?> GetEquityAsync(int equityId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            var entity = await db.Equities
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EquityId == equityId, ct);

            return entity?.ToEquity();
        }

        public async Task<Equity?> GetEquityByTickerAsync(string ticker, CancellationToken ct = default)
        {
            using var ctx = await _factory.CreateDbContextAsync(ct);
            var entity = await ctx.Equities
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Ticker == ticker, ct);

            return entity?.ToEquity();
        }

        public async Task<Equity> GetOrCreateEquityAsync(string ticker, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            using var tx = await db.Database.BeginTransactionAsync(ct);

            var entity = await db.Equities
                .FirstOrDefaultAsync(e => e.Ticker == ticker, ct);

            if (entity == null)
            {
                entity = new EquityEntity { Ticker = ticker };
                db.Equities.Add(entity);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            else
            {
                await tx.CommitAsync(ct);
            }

            return entity.ToEquity();
        }
    }
}