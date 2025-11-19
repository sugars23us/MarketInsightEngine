// MarketInsight.Application/Engine/IngestionWorker.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Services;
using MarketInsight.Shared.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketInsight.Application.Engine
{
    public interface IMarketBarSource
    {
        IAsyncEnumerable<MarketBar> ReadAllAsync(CancellationToken cancellationToken);
    }

    public sealed class IngestionWorker : BackgroundService
    {
        private readonly ILogger<IngestionWorker> _logger;
        private readonly IndicatorEngine _engine;
        private readonly IIndicatorSink _indicatorSink;
        private readonly IBarSink _barSink;
        private readonly IMarketBarSource _marketBarSource;
        private readonly IStockRegistry _stockRegistry;

        public IngestionWorker(
            ILogger<IngestionWorker> logger,
            IndicatorEngine engine,
            IIndicatorSink indicatorSink,
            IBarSink barSink,
            IMarketBarSource source,
            IStockRegistry stockRegistry)
        {
            _logger = logger;
            _engine = engine;
            _indicatorSink = indicatorSink;
            _barSink = barSink;
            _marketBarSource = source;
            _stockRegistry = stockRegistry;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IngestionWorker started.");

            // cache metadata by StockId to avoid calling DB per bar
            var tickerMetadataCache = new Dictionary<int, TickerMeta?>();

            try
            {
                await foreach (var bar in _marketBarSource.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    // 1) Persist raw bar. TODO: Optimize so we can pass a collection
                    await _barSink.UpsertAsync(new[] { bar }, stoppingToken).ConfigureAwait(false);

                    // 2) Get/calc metadata
                    if (!tickerMetadataCache.TryGetValue(bar.StockId, out var tickerMeta))
                    {
                        tickerMeta = await _stockRegistry.GetTickerMetaAsync(bar.StockId, stoppingToken)
                                                   .ConfigureAwait(false);
                        
                        tickerMetadataCache[bar.StockId] = tickerMeta;
                    }

                    // 3) Run indicators
                    var outputs = _engine.ProcessBar(bar, tickerMeta);
                    
                    if (outputs.Count > 0)
                    {
                        await _indicatorSink.UpsertAsync(outputs, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("IngestionWorker stopping (cancellation requested)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in IngestionWorker");
                throw;
            }
            finally
            {
                _logger.LogInformation("IngestionWorker stopped");
            }
        }
    }
}
