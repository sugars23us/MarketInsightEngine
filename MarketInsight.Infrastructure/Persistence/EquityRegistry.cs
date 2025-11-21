// MarketInsight.Infrastructure/Services/EquityRegistry.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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

        // Lifetime cache: EquityId → Equity metadata
        private readonly ConcurrentDictionary<int, Equity> _cache = new();

        public EquityRegistry(
            IDbContextFactory<MarketDbContext> factory,
            ILogger<EquityRegistry> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<Equity?> GetEquityAsync(int equityId, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(equityId, out var cached))
                return cached;

            var equity = await LoadAndCacheAsync(equityId, ct);
            return equity;
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
            using var ctx = await _factory.CreateDbContextAsync(ct);
            using var transaction = await ctx.Database.BeginTransactionAsync(ct);

            try
            {
                var existing = await ctx.Equities
                    .FirstOrDefaultAsync(e => e.Ticker == ticker, ct);

                if (existing != null)
                {
                    await transaction.CommitAsync(ct);
                    return existing.ToEquity();
                }

                var newEquity = new EquityEntity
                {
                    Ticker = ticker,
                    // Other fields default to null — will be filled later by refresh job
                };

                ctx.Equities.Add(newEquity);
                await ctx.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var result = newEquity.ToEquity();
                _cache[newEquity.EquityId] = result;

                _logger.LogInformation("Created new equity record for ticker {Ticker} (EquityId={Id})", ticker, newEquity.EquityId);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to create equity for ticker {Ticker}", ticker);
                throw;
            }
        }

        private async Task<Equity?> LoadAndCacheAsync(int equityId, CancellationToken ct)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync(ct);

                var entity = await db.Equities
                    .AsNoTracking()
                    .SingleOrDefaultAsync(e => e.EquityId == equityId, ct);

                if (entity == null)
                {
                    _logger.LogWarning("Equity metadata not found for EquityId={EquityId}", equityId);
                    return null;
                }

                var equity = entity.ToEquity();
                _cache[equityId] = equity;
                return equity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading equity metadata for EquityId={EquityId}", equityId);
                return null;
            }
        }
    }
}