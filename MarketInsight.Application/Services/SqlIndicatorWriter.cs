using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MarketInsight.Application.Engine;
using MarketInsight.Shared.Options;

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// SQL-based implementation of IIndicatorSink using TVP and dbo.UpsertIndicatorValues.
    /// </summary>
    public sealed class SqlIndicatorWriter : IIndicatorSink
    {
        private readonly string _cs;
        private readonly ILogger<SqlIndicatorWriter> _log;

        public SqlIndicatorWriter(IOptions<DatabaseOptions> opts, ILogger<SqlIndicatorWriter> log)
        {
            _cs = opts.Value.ConnectionString ?? throw new ArgumentNullException(nameof(opts));
            _log = log;
        }

        public async Task UpsertAsync(IReadOnlyCollection<IndicatorValue> batch, CancellationToken cancellationToken = default)
        {
            if (batch is null || batch.Count == 0)
                return;

            var tvp = BuildTvp(batch);

            using var con = new SqlConnection(_cs);
            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.UpsertIndicatorValues";
            cmd.CommandTimeout = 60;

            var p = cmd.Parameters.AddWithValue("@Rows", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.IndicatorValueTvp";

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _log.LogError(ex, "Error upserting {Count} indicator rows.", batch.Count);
                throw;
            }
        }

        private static DataTable BuildTvp(IReadOnlyCollection<IndicatorValue> batch)
        {
            var t = new DataTable();
            t.Columns.Add("StockId", typeof(int));
            t.Columns.Add("TimeframeId", typeof(short));
            t.Columns.Add("TsUtc", typeof(DateTime));
            t.Columns.Add("MetricCode", typeof(string));
            t.Columns.Add("Period", typeof(short));
            t.Columns.Add("ParamsJson", typeof(string));
            t.Columns.Add("Value", typeof(decimal));

            foreach (var iv in batch)
            {
                var row = t.NewRow();
                row["StockId"] = iv.StockId;
                row["TimeframeId"] = (short)iv.TimeframeId;
                var ts = DateTime.SpecifyKind(iv.TsUtc, DateTimeKind.Utc);
                row["TsUtc"] = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, DateTimeKind.Utc);
                row["MetricCode"] = iv.MetricCode;
                row["Period"] = iv.Period;
                row["ParamsJson"] = (object?)iv.ParamsJson ?? DBNull.Value;
                row["Value"] = iv.Value;
                t.Rows.Add(row);
            }

            return t;
        }
    }
}
