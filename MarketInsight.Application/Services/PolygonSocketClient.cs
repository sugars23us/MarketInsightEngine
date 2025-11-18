using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarketInsight.Application.Engine;
using MarketInsight.Shared.Options;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// Polygon WebSocket adapter implementing IMarketBarSource.
    /// </summary>
    public sealed class PolygonSocketClient : IMarketBarSource, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, int> _symbolToIdCache = new();
        private readonly PolygonOptions _opt;
        private readonly IStockRegistry _stockRegistry;
        private readonly ILogger<PolygonSocketClient> _logger;

        public PolygonSocketClient(IStockRegistry stockRegistry, IOptions<PolygonOptions> opt, ILogger<PolygonSocketClient> logger)
        {
            _opt = opt.Value;
            _stockRegistry = stockRegistry;
            _logger = logger;
        }

        // MarketInsight.Application.Services.PolygonSocketClient.cs
        public async IAsyncEnumerable<MarketBar> ReadAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var ws = new ClientWebSocket();

                // We let exceptions bubble up (they'll stop the worker) or you can wrap the whole loop at a higher level.
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                await ws.ConnectAsync(new Uri(_opt.StocksWsUrl), cancellationToken).ConfigureAwait(false);

                await SendAsync(ws, $@"{{""action"":""auth"",""params"":""{_opt.ApiKey}""}}", cancellationToken);
                await SendAsync(ws, $@"{{""action"":""subscribe"",""params"":""{_opt.Symbols}""}}", cancellationToken);

                _logger.LogInformation("Connected & subscribed to Polygon: {Params}", _opt.Symbols);

                var buffer = new byte[64 * 1024];

                while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogWarning("Polygon socket closed: {Status} {Desc}",
                                result.CloseStatus, result.CloseStatusDescription);
                            break;
                        }

                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (ms.Length == 0)
                        continue;

                    var json = Encoding.UTF8.GetString(ms.ToArray());

                    await foreach (var bar in ParseBarsAsync(json, cancellationToken))
                        yield return bar;   // ✅ now allowed (no catch/finally in scope)
                }

                // optional delay before reconnect (if server closed)
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Polygon connection ended, reconnecting in 5 seconds…");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task SendAsync(ClientWebSocket ws, string msg, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }

        private async IAsyncEnumerable<MarketBar> ParseBarsAsync(string json, [EnumeratorCancellation] CancellationToken ct)
        {
            // Parse outside of try/catch – JsonDocument.Parse throws immediately if invalid
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Polygon JSON batch – skipping entire message");
                yield break;
            }

            // From here on, doc is valid → safe to enumerate
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                doc.Dispose();
                yield break;
            }

            var stockIdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();

                if (!el.TryGetProperty("ev", out var evProp) || evProp.GetString() != "AM")
                    continue;

                var sym = el.GetProperty("sym").GetString() ?? string.Empty;
                if (string.IsNullOrEmpty(sym))
                    continue;

                // Resolve StockId – cached per batch
                if (!stockIdCache.TryGetValue(sym, out var stockId))
                {
                    stockId = await ResolveStockIdAsync(sym, ct);
                    stockIdCache[sym] = stockId;
                }

                var o = el.GetProperty("o").GetDecimal();
                var h = el.GetProperty("h").GetDecimal();
                var l = el.GetProperty("l").GetDecimal();
                var c = el.GetProperty("c").GetDecimal();
                var v = el.GetProperty("v").GetInt64();
                var vw = el.TryGetProperty("vw", out var vwProp) ? vwProp.GetDecimal() : 0m;
                var e = el.GetProperty("e").GetInt64();
                var tsUtc = DateTimeOffset.FromUnixTimeMilliseconds(e).UtcDateTime;

                yield return new MarketBar(
                    stockId,
                    1,          // 1-minute
                    tsUtc,
                    o, h, l, c,
                    v,
                    vw,
                    null        // TradeCount not available
                );
            }

            doc.Dispose();
        }

        private async Task<int> ResolveStockIdAsync(string symbol, CancellationToken ct)
        {
            if (_symbolToIdCache.TryGetValue(symbol, out var id))
                return id;

            var meta = await _stockRegistry.GetTickerMetaAsync(symbol, ct)
                       ?? await _stockRegistry.CreateTickerMetaIfMissingAsync(symbol, ct);
            _symbolToIdCache[symbol] = meta.StockId;
            _logger.LogInformation("Resolved/created StockId {Id} for {Symbol}", meta.StockId, symbol);
            return meta.StockId;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
