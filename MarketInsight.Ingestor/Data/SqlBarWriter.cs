using MarketInsight.Ingestor.AppOptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace MarketInsight.Ingestor.Data;

public sealed class SqlBarWriter
{
    private readonly string _cs;

    public SqlBarWriter(IOptions<DatabaseOptions> db) => _cs = db.Value.ConnectionString;

    public async Task UpsertAsync(IReadOnlyCollection<BarRecord> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        using var con = new SqlConnection(_cs);
        using var cmd = new SqlCommand("dbo.BulkUpsertBars", con) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.Add(batch.ToTvp());
        await con.OpenAsync(ct);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
