using System.Collections.Concurrent;
using System.Text.Json;
using MarketInsight.Ingestor.AppOptions;
using MarketInsight.Ingestor.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Linq; // for ElementAt

namespace MarketInsight.Ingestor.Ingestion;

public sealed class IngestionWorker : BackgroundService
{
    private readonly ILogger<IngestionWorker> _log;
    private readonly PolygonSocketClient _socket;
    private readonly StockRegistry _stocks;
    private readonly SqlBarWriter _writer;
    private readonly IngestionOptions _opt;

    private readonly SqlIndicatorWriter _indWriter;
    private readonly Dictionary<int, AtsWindows> _state = new(); // per StockId

    private readonly List<IndicatorRecord> _indBatch = new();
    private readonly Dictionary<int, DateTime> _lastTsUtc = new();


    public IngestionWorker(
      ILogger<IngestionWorker> log,
      PolygonSocketClient socket,
      StockRegistry stocks,
      SqlBarWriter writer,
      SqlIndicatorWriter indWriter,
      IOptions<IngestionOptions> opt)
    {
        _log = log; _socket = socket; _stocks = stocks; _writer = writer; _indWriter = indWriter; _opt = opt.Value;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = Environment.GetEnvironmentVariable("SUBSCRIBE")
                      ?? "AM.AAPL,AM.MSFT,AM.TSLA";

        var tickers = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim().Replace("AM.", ""))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToArray();

        await _stocks.WarmAsync(tickers, stoppingToken);

        // Build a jittered backoff sequence; we’ll clamp each delay to a max.
        var baseDelay = TimeSpan.FromMilliseconds(_opt.ReconnectBaseDelayMs);
        var maxDelay = TimeSpan.FromMilliseconds(_opt.ReconnectMaxDelayMs);
        var seq = Backoff.DecorrelatedJitterBackoffV2(baseDelay, retryCount: 100, fastFirst: true);

        // Provider that returns the Nth jittered delay, capped at maxDelay.
        TimeSpan SleepProvider(int attempt)
        {
            var d = seq.ElementAt(Math.Min(attempt, 99));
            return d > maxDelay ? maxDelay : d;
        }

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: attempt => SleepProvider(attempt),
                onRetryAsync: (ex, ts, ctx) =>
                {
                    _log.LogWarning(ex, "Reconnecting in {Delay}", ts);
                    return Task.CompletedTask; // IMPORTANT: return a Task
                });

        await policy.ExecuteAsync(ct => RunLoopAsync(ct), stoppingToken);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _socket.ConnectAndAuthAsync(ct);

        var batch = new List<BarRecord>(_opt.BatchSize);
        var nextFlush = DateTime.UtcNow.AddSeconds(_opt.FlushSeconds);

        await foreach (var msg in _socket.ReadAggregatesAsync(ct))
        {
            var stockId = await _stocks.GetOrCreateAsync(msg.Symbol!, ct);
            var tsUtc = DateTimeOffset.FromUnixTimeMilliseconds(msg.EndEpochMs).UtcDateTime;

            batch.Add(new BarRecord(
                StockId: stockId,
                TimeframeId: _opt.TimeframeIdMinute,
                TsUtc: tsUtc,
                Open: msg.Open, High: msg.High, Low: msg.Low, Close: msg.Close,
                Volume: msg.Volume, Vwap: msg.Vwap, TradeCount: msg.TradeCount));


            // --- Detect trading day rollover (reset rolling stats per stock) ---
            if (_lastTsUtc.TryGetValue(stockId, out var lastTs))
            {
                // If the date (UTC) changed since last bar, reset the rolling windows
                if (tsUtc.Date != lastTs.Date)
                {
                    _log.LogInformation("Resetting ATS windows for {StockId} due to new trading day", stockId);
                    _state[stockId] = new AtsWindows(); // resets both MA15 and STD60 buffers
                }
            }
            _lastTsUtc[stockId] = tsUtc;



            // 1) Make sure per-stock state exists
            // Ensure per-stock state exists
            if (!_state.TryGetValue(stockId, out var win))
            {
                win = new AtsWindows();
                _state[stockId] = win;
            }

            // ---- Inputs from the bar (guards for NaN/div-by-zero)
            double? _ats = (msg.TradeCount.GetValueOrDefault() > 0)
                ? (double)msg.Volume / msg.TradeCount.Value
                : (double?)null;

            double? _vol = (double)msg.Volume;

            double? _vwap = msg.Vwap.HasValue && msg.Vwap.Value > 0 ? (double?)msg.Vwap.Value : null;
            double? _close = (double?)msg.Close;

            double? _vwapDev = (_vwap.HasValue && _vwap.Value > 0 && _close.HasValue)
                ? (_close.Value - _vwap.Value) / _vwap.Value
                : (double?)null;

            // ---- Feed windows
            if (_ats.HasValue)
            {
                win.Ma15.Add(_ats.Value);
                win.Stats15.Add(_ats.Value);
                win.Stats60.Add(_ats.Value);
            }
            if (_vol.HasValue) win.Vol60.Add(_vol.Value);
            if (_vwapDev.HasValue) win.Dev60.Add(_vwapDev.Value);

            // ---- Helpers
            static double Z(double x, double mean, double std)
                => (std > 0) ? (x - mean) / std : double.NaN;

            void AddInd(string code, short period, double val)
            {
                if (double.IsNaN(val)) return;
                _indBatch.Add(new IndicatorRecord(
                    StockId: stockId,
                    TimeframeId: _opt.TimeframeIdMinute, // 1 for 1-minute
                    TsUtc: tsUtc,
                    MetricCode: code,
                    Period: period,
                    Value: (decimal)val
                ));
            }

            // ---- Point values & rolling stats
            double ats = _ats ?? double.NaN;
            double atsMa15 = win.Ma15.Mean;
            double atsMa60 = win.Stats60.Mean;

            double z15 = double.NaN;
            if (_ats.HasValue && !double.IsNaN(win.Stats15.Mean) && !double.IsNaN(win.Stats15.StdSample))
                z15 = Z(_ats.Value, win.Stats15.Mean, win.Stats15.StdSample);

            double z60 = double.NaN;
            if (_ats.HasValue && !double.IsNaN(win.Stats60.Mean) && !double.IsNaN(win.Stats60.StdSample))
                z60 = Z(_ats.Value, win.Stats60.Mean, win.Stats60.StdSample);

            // ---- IFI_60 (directional)
            double ifi60 = double.NaN;
            if (_vwapDev.HasValue
                && !double.IsNaN(win.Dev60.Mean) && !double.IsNaN(win.Dev60.StdSample)
                && !double.IsNaN(win.Vol60.Mean) && !double.IsNaN(win.Vol60.StdSample)
                && _vol.HasValue && _ats.HasValue
                && !double.IsNaN(z60))
            {
                var devZ60 = Z(_vwapDev.Value, win.Dev60.Mean, win.Dev60.StdSample);
                var volZ60 = Z(_vol.Value, win.Vol60.Mean, win.Vol60.StdSample);
                if (!double.IsNaN(devZ60) && !double.IsNaN(volZ60))
                {
                    // Street-standard weights: 0.5 Dev, 0.3 Vol, 0.2 ATS
                    var sign = Math.Sign(_vwapDev.Value); // below VWAP -> negative -> will flip to positive after sign*magnitude
                    var magnitude = 0.5 * Math.Abs(devZ60)
                                  + 0.3 * volZ60
                                  + 0.2 * z60; // use ATS_Z_60 here
                    ifi60 = sign * magnitude;
                }
            }

            // ---- Enqueue indicator rows (names include frequency)
            AddInd("ATS", 0, ats);       // per-bar ATS
            AddInd("ATS_MA_15", 15, atsMa15);
            AddInd("ATS_MA_60", 60, atsMa60);
            AddInd("ATS_Z_15", 15, z15);
            AddInd("ATS_Z_60", 60, z60);
            AddInd("IFI_60", 60, ifi60);

            if (batch.Count >= _opt.BatchSize || _indBatch.Count >= _opt.BatchSize || DateTime.UtcNow >= nextFlush)
            {
                try
                {
                    await _writer.UpsertAsync(batch, ct);
                    batch.Clear();

                    if (_indBatch.Count > 0)
                    {
                        await _indWriter.UpsertAsync(_indBatch, ct);
                        _indBatch.Clear();
                    }

                    nextFlush = DateTime.UtcNow.AddSeconds(_opt.FlushSeconds);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Flush failed");
                    await Task.Delay(500, ct);
                }
            }

        }

        // final flush on exit
        if (batch.Count > 0)
        {
            try { await _writer.UpsertAsync(batch, ct); } catch { /* swallow on shutdown */ }
            batch.Clear();
        }

        if (_indBatch.Count > 0)
        {
            try { await _indWriter.UpsertAsync(_indBatch, ct); } catch { }
            _indBatch.Clear();
        }

        throw new InvalidOperationException("Socket closed unexpectedly (will reconnect).");
    }
}
