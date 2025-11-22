// MarketInsight.Application/Engine/IngestionWorker.cs
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.DTOs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketInsight.Application.Engine
{
    public sealed class IngestionWorker : BackgroundService
    {
        private readonly ILogger<IngestionWorker> _logger;
        private readonly IndicatorEngine _engine;
        private readonly IEquityIndicatorSink _indicatorSink;
        private readonly IEquityCandleSink _candleSink;
        private readonly IEquityCandleSource _candleSource;
        private readonly IEquityRegistry _equityRegistry;
        private readonly CachedRepository<int, Equity> _entityCache = new();

        public IngestionWorker(
            ILogger<IngestionWorker> logger,
            IndicatorEngine engine,
            IEquityIndicatorSink indicatorSink,
            IEquityCandleSink candleSink,
            IEquityCandleSource source,
            IEquityRegistry equityRegistry)
        {
            _logger = logger;
            _engine = engine;
            _indicatorSink = indicatorSink;
            _candleSink = candleSink;
            _candleSource = source;
            _equityRegistry = equityRegistry;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IngestionWorker started.");

            try
            {
                await foreach (var candle in _candleSource.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    // 1. Persist raw candle
                    await _candleSink.UpsertAsync(new[] { candle }, stoppingToken).ConfigureAwait(false);

                    // 2. Get equity metadata (float, ADV, etc.)
                    var equity = await _entityCache.GetOrLoadAsync(candle.EquityId, _equityRegistry.GetEquityAsync, stoppingToken);

                    // 3. Run indicators
                    var indicators = _engine.ProcessCandle(candle, equity);

                    if (indicators.Count > 0)
                    {
                        await _indicatorSink.UpsertAsync(indicators, stoppingToken).ConfigureAwait(false);
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
