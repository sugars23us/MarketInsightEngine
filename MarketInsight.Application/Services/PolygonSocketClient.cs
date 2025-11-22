// MarketInsight.Infrastructure.Streaming/PolygonSocketClient.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.DTOs;
using MarketInsight.Shared.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketInsight.Infrastructure.Streaming
{
    /// <summary>
    /// Polygon WebSocket real-time source → yields EquityCandle records.
    /// Implements IEquityCandleSource.
    /// </summary>
    public sealed class PolygonSocketClient : IEquityCandleSource, IAsyncDisposable
    {
        private readonly PolygonOptions _opt;
        private readonly IEquityRegistry _equityRegistry;
        private readonly CachedRepository<string, Equity> _tickerCache;
        private readonly ILogger<PolygonSocketClient> _logger;

        public PolygonSocketClient(
            IOptions<PolygonOptions> options,
            IEquityRegistry equityRegistry,
            CachedRepository<string, Equity> tickerCache,
            ILogger<PolygonSocketClient> logger)
        {
            _opt = options.Value;
            _equityRegistry = equityRegistry;
            _tickerCache = tickerCache;
            _logger = logger;
        }

        public async IAsyncEnumerable<EquityCandle> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                using var ws = new ClientWebSocket();
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                await ws.ConnectAsync(new Uri(_opt.StocksWsUrl), ct).ConfigureAwait(false);

                await SendAsync(ws, $@"{{""action"":""auth"",""params"":""{_opt.ApiKey}""}}", ct);
                await SendAsync(ws, $@"{{""action"":""subscribe"",""params"":""AM.{_opt.Symbols}""}}", ct);

                _logger.LogInformation("Connected & subscribed to Polygon: AM.{Symbols}", _opt.Symbols);

                var buffer = new byte[64 * 1024];

                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var json = await ReceiveFullMessageAsync(ws, buffer, ct);
                    if (json == null) break;

                    await foreach (var candle in ParseCandlesAsync(json, ct))
                        yield return candle;
                }                
            }
        }

        private static async Task SendAsync(ClientWebSocket ws, string msg, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }

        private static async Task<string?> ReceiveFullMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            while (true)
            {
                var result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                ms.Write(buffer, 0, result.Count);
                if (result.EndOfMessage) break;
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private async IAsyncEnumerable<EquityCandle> ParseCandlesAsync(
            string json,
            [EnumeratorCancellation] CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(json))
                yield break;

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                yield break;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();

                if (!el.TryGetProperty("ev", out var ev) || ev.GetString() != "AM")
                    continue;

                var sym = el.GetProperty("sym").GetString()!;
                if (string.IsNullOrEmpty(sym))
                    continue;

                var equity = await _tickerCache.GetOrLoadAsync(sym, _equityRegistry.GetOrCreateEquityAsync, ct);
                var equityId = equity.EquityId;

                // Safe extraction of all fields
                decimal open = GetDecimal(el, "o");
                decimal high = GetDecimal(el, "h");
                decimal low = GetDecimal(el, "l");
                decimal close = GetDecimal(el, "c");
                long volume = GetInt64(el, "v");
                decimal? vwap = TryGetDecimal(el, "a");   // session VWAP
                long? ats = TryGetInt64(el, "z");     // average trade size

                var tsUtc = DateTimeOffset.FromUnixTimeMilliseconds(GetInt64(el, "e")).UtcDateTime;

                yield return new EquityCandle(
                    EquityId: equityId,
                    TimeframeId: 1,
                    TsUtc: tsUtc,
                    Open: open,
                    High: high,
                    Low: low,
                    Close: close,
                    Volume: volume,
                    Vwap: vwap,
                    Ats: ats
                );
            }
        }

        private static decimal GetDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : 0m;

        private static decimal? TryGetDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : null;

        private static long GetInt64(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : 0L;

        private static long? TryGetInt64(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}