// MarketInsight.Infrastructure.Persistence/SqlEquityIndicatorWriter.cs
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
using MarketInsight.Application.Interfaces;

namespace MarketInsight.Infrastructure.Persistence
{
    /// <summary>
    /// SQL-based implementation of IEquityIndicatorSink using TVP and dbo.UpsertEquityIndicators.
    /// </summary>
    public sealed class SqlEquityIndicatorWriter : IEquityIndicatorSink
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlEquityIndicatorWriter> _logger;

        public SqlEquityIndicatorWriter(
            IOptions<DatabaseOptions> options,
            ILogger<SqlEquityIndicatorWriter> logger)
        {
            _connectionString = options.Value.ConnectionString
                ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public async Task UpsertAsync(
            IReadOnlyCollection<EquityIndicator> indicators,
            CancellationToken ct = default)
        {
            if (indicators is null || indicators.Count == 0)
                return;

            var tvp = BuildTvp(indicators);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.UpsertEquityIndicators";  // ← new proc
            command.CommandTimeout = 60;

            var parameter = command.Parameters.AddWithValue("@tvp", tvp);
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.TypeName = "dbo.EquityIndicatorTvp";       // ← new TVP

            try
            {
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                _logger.LogDebug("Upserted {Count} equity indicators", indicators.Count);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error upserting {Count} equity indicators", indicators.Count);
                throw;
            }
        }

        private static DataTable BuildTvp(IReadOnlyCollection<EquityIndicator> indicators)
        {
            var table = new DataTable();
            table.Columns.Add("EquityId", typeof(int));
            table.Columns.Add("TimeframeId", typeof(byte));
            table.Columns.Add("TsUtc", typeof(DateTime));
            table.Columns.Add("MetricCode", typeof(string));
            table.Columns.Add("Period", typeof(short));
            table.Columns.Add("Value", typeof(decimal));
            table.Columns.Add("ParamsJson", typeof(string));

            foreach (var i in indicators)
            {
                var row = table.NewRow();
                row["EquityId"] = i.EquityId;
                row["TimeframeId"] = i.TimeframeId;
                row["TsUtc"] = DateTime.SpecifyKind(i.TsUtc, DateTimeKind.Utc);
                row["MetricCode"] = i.MetricCode;
                row["Period"] = i.Period;
                row["Value"] = i.Value;
                row["ParamsJson"] = (object?)i.ParamsJson ?? DBNull.Value;

                table.Rows.Add(row);
            }

            return table;
        }
    }
}