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

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// Polygon WebSocket adapter implementing IMarketBarSource.
    /// </summary>
    public sealed class PolygonSocketClient : IMarketBarSource, IAsyncDisposable
    {
        private readonly PolygonOptions _opt;
        private readonly ILogger<PolygonSocketClient> _log;

        public PolygonSocketClient(IOptions<PolygonOptions> opt, ILogger<PolygonSocketClient> log)
        {
            _opt = opt.Value;
            _log = log;
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

                _log.LogInformation("Connected & subscribed to Polygon: {Params}", _opt.Symbols);

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
                            _log.LogWarning("Polygon socket closed: {Status} {Desc}",
                                result.CloseStatus, result.CloseStatusDescription);
                            break;
                        }

                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (ms.Length == 0)
                        continue;

                    var json = Encoding.UTF8.GetString(ms.ToArray());

                    foreach (var bar in ParseBars(json))
                        yield return bar;   // ✅ now allowed (no catch/finally in scope)
                }

                // optional delay before reconnect (if server closed)
                if (!cancellationToken.IsCancellationRequested)
                {
                    _log.LogInformation("Polygon connection ended, reconnecting in 5 seconds…");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task SendAsync(ClientWebSocket ws, string msg, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }

        private IEnumerable<MarketBar> ParseBars(string json)
        {
            var list = new List<MarketBar>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return list;

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (!el.TryGetProperty("ev", out var evProp) || evProp.GetString() != "AM")
                        continue;

                    var sym = el.GetProperty("sym").GetString() ?? string.Empty;

                    // In a real system, map sym -> StockId via StockRegistry.
                    var stockId = sym.GetHashCode();

                    var o = el.GetProperty("o").GetDecimal();
                    var h = el.GetProperty("h").GetDecimal();
                    var l = el.GetProperty("l").GetDecimal();
                    var c = el.GetProperty("c").GetDecimal();
                    var v = el.GetProperty("v").GetInt64();
                    var vw = el.TryGetProperty("vw", out var vwProp) ? vwProp.GetDecimal() : 0m;
                    var e = el.GetProperty("e").GetInt64();

                    var tsUtc = DateTimeOffset.FromUnixTimeMilliseconds(e).UtcDateTime;

                    list.Add(new MarketBar(
                        stockId,
                        1,          // 1-minute timeframe
                        tsUtc,
                        o,
                        h,
                        l,
                        c,
                        v,
                        vw,
                        null        // TradeCount not provided by AM stream
                    ));
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to parse Polygon JSON: {Json}", json);
            }

            return list;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
