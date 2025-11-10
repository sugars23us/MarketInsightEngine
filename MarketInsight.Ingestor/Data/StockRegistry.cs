using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Dapper;
using MarketInsight.Ingestor.AppOptions;

namespace MarketInsight.Ingestor.Data;

public sealed class StockRegistry
{
    private readonly string _cs;
    private readonly Dictionary<string, int> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public StockRegistry(IOptions<DatabaseOptions> db)
    {
        _cs = db.Value.ConnectionString;
    }

    public async Task WarmAsync(IEnumerable<string> tickers, CancellationToken ct)
    {
        foreach (var t in tickers)
            _ = await GetOrCreateAsync(t, ct);
    }

    public async Task<int> GetOrCreateAsync(string ticker, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(ticker, out var id))
                return id;
        }

        using var con = new SqlConnection(_cs);
        var idNew = await con.ExecuteScalarAsync<int>(new CommandDefinition(@"
MERGE dbo.STOCK AS t
USING (SELECT @Ticker AS Ticker) AS s
ON (t.Ticker = s.Ticker)
WHEN NOT MATCHED THEN INSERT (Ticker) VALUES (s.Ticker)
WHEN MATCHED THEN UPDATE SET Ticker = t.Ticker
OUTPUT inserted.StockId;",
            new { Ticker = ticker }, cancellationToken: ct));

        lock (_gate) _cache[ticker] = idNew;
        return idNew;
    }
}
