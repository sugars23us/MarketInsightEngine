using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// SQL-based implementation of IEquityCandleSink using TVP (dbo.EquityCandleTvp)
    /// and an upsert stored procedure dbo.UpsertEquityCandles.
    /// </summary>
    public sealed class SqlEquityCandleWriter : IEquityCandleSink
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlEquityCandleWriter> _log;

        public SqlEquityCandleWriter(IOptions<DatabaseOptions> dbOptions,
                            ILogger<SqlEquityCandleWriter> log)
        {
            _connectionString = dbOptions.Value.ConnectionString
                                ?? throw new ArgumentNullException(nameof(dbOptions));
            _log = log;
        }

        public async Task UpsertAsync(IReadOnlyCollection<EquityCandle> candles,
                              CancellationToken ct = default)
        {
            if (candles is null || candles.Count == 0)
                return;

            var tvp = BuildTvp(candles);

            await using var con = new SqlConnection(_connectionString);
            await con.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.UpsertEquityCandles";
            cmd.CommandTimeout = 60;

            var p = cmd.Parameters.AddWithValue("@tvp", tvp);
            p.SqlDbType = SqlDbType.Structured;
            p.TypeName = "dbo.EquityCandleTvp";

            try
            {
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                _log.LogDebug("Upserted {Count} equity candles", candles.Count);
            }
            catch (SqlException ex)
            {
                _log.LogError(ex, "Error upserting {Count} equity candles", candles.Count);
                throw;
            }
        }

        private static DataTable BuildTvp(IReadOnlyCollection<EquityCandle> candles)
        {
            var dt = new DataTable();
            dt.Columns.Add("EquityId", typeof(int));
            dt.Columns.Add("TimeframeId", typeof(byte));
            dt.Columns.Add("TsUtc", typeof(DateTime));
            dt.Columns.Add("Open", typeof(decimal));
            dt.Columns.Add("High", typeof(decimal));
            dt.Columns.Add("Low", typeof(decimal));
            dt.Columns.Add("Close", typeof(decimal));
            dt.Columns.Add("Volume", typeof(long));
            dt.Columns.Add("Vwap", typeof(decimal));
            dt.Columns.Add("Ats", typeof(long));

            foreach (var c in candles)
            {
                // Ensure TsUtc is UTC and second-precision (matches DATETIME2(0))
                var tsUtc = new DateTime(c.TsUtc.Year, c.TsUtc.Month, c.TsUtc.Day,
                                        c.TsUtc.Hour, c.TsUtc.Minute, c.TsUtc.Second,
                                        DateTimeKind.Utc);

                var row = dt.NewRow();
                row["EquityId"] = c.EquityId;
                row["TimeframeId"] = c.TimeframeId;
                row["TsUtc"] = tsUtc;
                row["Open"] = c.Open;
                row["High"] = c.High;
                row["Low"] = c.Low;
                row["Close"] = c.Close;
                row["Volume"] = c.Volume;
                row["Vwap"] = c.Vwap ?? (object)DBNull.Value;
                row["Ats"] = c.Ats ?? (object)DBNull.Value;

                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
