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
        private readonly IMarketBarSource _marketBarSource;
        private readonly IStockRegistry _stockRegistry;

        public IngestionWorker(
            ILogger<IngestionWorker> logger,
            IndicatorEngine engine,
            IIndicatorSink sink,
            IMarketBarSource source,
            IStockRegistry stockRegistry)
        {
            _logger = logger;
            _engine = engine;
            _indicatorSink = sink;
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
                    if (!tickerMetadataCache.TryGetValue(bar.StockId, out var tickerMeta))
                    {
                        tickerMeta = await _stockRegistry.GetMetaAsync(bar.StockId, stoppingToken)
                                                   .ConfigureAwait(false);
                        
                        tickerMetadataCache[bar.StockId] = tickerMeta;
                    }

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
