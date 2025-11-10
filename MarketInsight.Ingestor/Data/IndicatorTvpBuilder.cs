using System.Data;
using Microsoft.Data.SqlClient;

namespace MarketInsight.Ingestor.Data;

public static class IndicatorTvpBuilder
{
    public static SqlParameter ToTvp(this IEnumerable<IndicatorRecord> rows)
    {
        var t = new DataTable();
        t.Columns.Add("StockId", typeof(int));
        t.Columns.Add("TimeframeId", typeof(byte));
        t.Columns.Add("TsUtc", typeof(DateTime));
        t.Columns.Add("MetricCode", typeof(string));
        t.Columns.Add("Period", typeof(short));
        t.Columns.Add("ParamsJson", typeof(string));
        t.Columns.Add("Value", typeof(decimal));

        foreach (var r in rows)
            t.Rows.Add(r.StockId, r.TimeframeId, r.TsUtc, r.MetricCode, (object?)r.Period ?? DBNull.Value,
                       (object?)r.ParamsJson ?? DBNull.Value, r.Value);

        return new SqlParameter("@Indicators", t)
        {
            SqlDbType = SqlDbType.Structured,
            TypeName = "dbo.IndicatorUpsertTvp"
        };
    }
}
