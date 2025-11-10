using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace MarketInsight.Ingestor.Data;

public sealed class SqlIndicatorWriter
{
    private readonly string _cs;
    public SqlIndicatorWriter(IOptions<AppOptions.DatabaseOptions> db) => _cs = db.Value.ConnectionString;

    public async Task UpsertAsync(IReadOnlyCollection<IndicatorRecord> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        // Build DataTable that matches dbo.Indicator_TVP exactly
        var tvp = new DataTable();
        tvp.Columns.Add("StockId", typeof(int));
        tvp.Columns.Add("TimeframeId", typeof(byte));
        tvp.Columns.Add("TsUtc", typeof(DateTime));
        tvp.Columns.Add("MetricCode", typeof(string));
        tvp.Columns.Add("Period", typeof(short));     // allow DBNull if null
        tvp.Columns.Add("ParamsJson", typeof(string));    // allow DBNull
        tvp.Columns.Add("Value", typeof(decimal));

        foreach (var r in batch)
        {
            tvp.Rows.Add(
                r.StockId,
                (byte)r.TimeframeId,
                r.TsUtc,                          // DateTime (UTC)
                r.MetricCode,
                (object?)r.Period ?? DBNull.Value,
                (object?)r.ParamsJson ?? DBNull.Value,
                r.Value                           // decimal(19,8), must NOT be null
            );
        }

        using var con = new SqlConnection(_cs);
        await con.OpenAsync(ct);

        using var cmd = new SqlCommand("dbo.UpsertIndicatorBatch", con)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add(new SqlParameter("@Indicators", SqlDbType.Structured)
        {
            TypeName = "dbo.Indicator_TVP",
            Value = tvp
        });

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
