using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Engine;
using MarketInsight.Shared.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// SQL-based implementation of IBarSink using TVP (dbo.BarTvp)
    /// and an upsert stored procedure dbo.UpsertBars.
    /// </summary>
    public sealed class SqlBarWriter : IBarSink
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlBarWriter> _log;

        public SqlBarWriter(IOptions<DatabaseOptions> dbOptions,
                            ILogger<SqlBarWriter> log)
        {
            _connectionString = dbOptions.Value.ConnectionString
                                ?? throw new ArgumentNullException(nameof(dbOptions));
            _log = log;
        }

        public async Task UpsertAsync(IReadOnlyCollection<MarketBar> bars,
                                      CancellationToken cancellationToken = default)
        {
            if (bars is null || bars.Count == 0)
                return;

            var tvp = BuildTvp(bars);

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.UpsertBars";
            cmd.CommandTimeout = 60;

            var p = cmd.Parameters.AddWithValue("@Rows", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.BarTvp";

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _log.LogError(ex, "Error upserting {Count} bars.", bars.Count);
                throw;
            }
        }

        private static DataTable BuildTvp(IReadOnlyCollection<MarketBar> bars)
        {
            var t = new DataTable();
            t.Columns.Add("StockId", typeof(int));
            t.Columns.Add("TimeframeId", typeof(short));
            t.Columns.Add("TsUtc", typeof(DateTime));
            t.Columns.Add("Open", typeof(decimal));
            t.Columns.Add("High", typeof(decimal));
            t.Columns.Add("Low", typeof(decimal));
            t.Columns.Add("Close", typeof(decimal));
            t.Columns.Add("Volume", typeof(long));
            t.Columns.Add("Vwap", typeof(decimal));
            t.Columns.Add("TradeCount", typeof(int));

            foreach (var bar in bars)
            {
                var ts = DateTime.SpecifyKind(bar.TsUtc, DateTimeKind.Utc);
                ts = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, DateTimeKind.Utc);

                var row = t.NewRow();
                row["StockId"] = bar.StockId;
                row["TimeframeId"] = (short)bar.TimeframeId;
                row["TsUtc"] = ts;
                row["Open"] = bar.Open;
                row["High"] = bar.High;
                row["Low"] = bar.Low;
                row["Close"] = bar.Close;
                row["Volume"] = bar.Volume;
                row["Vwap"] = bar.Vwap ?? 0m;
                row["TradeCount"] = (object?)bar.TradeCount ?? DBNull.Value;
                t.Rows.Add(row);
            }

            return t;
        }
    }
}
