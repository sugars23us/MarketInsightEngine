using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarketInsight.Ingestor.AppOptions;

namespace MarketInsight.Ingestor.Ingestion;

public sealed class PolygonSocketClient : IAsyncDisposable
{
    private ClientWebSocket? _ws;                       // note: nullable, recreated each attempt
    private readonly ILogger<PolygonSocketClient> _log;
    private readonly PolygonOptions _opt;

    public PolygonSocketClient(IOptions<PolygonOptions> opt, ILogger<PolygonSocketClient> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task ConnectAndAuthAsync(CancellationToken ct)
    {
        // Always dispose any previous socket before creating a new one
        await CloseAsync();

        var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        await ws.ConnectAsync(new Uri(_opt.WebSocketUrl), ct);

        // Authenticate + subscribe
        await SendAsync(ws, $@"{{""action"":""auth"",""params"":""{_opt.ApiKey}""}}", ct);
        await SendAsync(ws, $@"{{""action"":""subscribe"",""params"":""{_opt.Subscribe}""}}", ct);

        _ws = ws;                                       // publish the connected socket
    }

    public async IAsyncEnumerable<PolygonAggregateMessage> ReadAggregatesAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (_ws is null || _ws.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected.");

        var buffer = new byte[64 * 1024];
        var ms = new MemoryStream();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    throw new WebSocketException("Socket closed by server.");

                if (result.Count > 0)
                    ms.Write(buffer.AsSpan(0, result.Count));

                if (!result.EndOfMessage)
                    continue;

                var json = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.SetLength(0);

                List<PolygonAggregateMessage> batch = new();
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            if (el.TryGetProperty("ev", out var evProp) && evProp.GetString() == "AM")
                            {
                                var msg = el.Deserialize<PolygonAggregateMessage>();
                                if (msg is not null) batch.Add(msg);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to parse message: {Json}", json);
                }

                foreach (var item in batch) yield return item;

                if (_ws.State != WebSocketState.Open)
                    throw new WebSocketException("Socket not open.");
            }

            throw new OperationCanceledException(ct);
        }
        finally
        {
            ms.Dispose();
        }
    }

    private static Task SendAsync(ClientWebSocket ws, string text, CancellationToken ct) =>
        ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, ct);

    public async Task CloseAsync()
    {
        var ws = _ws;
        _ws = null;
        if (ws == null) return;

        try
        {
            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { /* ignore */ }
        finally
        {
            ws.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}
