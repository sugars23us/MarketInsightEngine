using System.Data;
using Microsoft.Data.SqlClient;

namespace MarketInsight.Ingestor.Data;

public static class BarTvpBuilder
{
    public static SqlParameter ToTvp(this IEnumerable<BarRecord> bars)
    {
        var table = new DataTable();
        table.Columns.Add("StockId", typeof(int));
        table.Columns.Add("TimeframeId", typeof(byte));
        table.Columns.Add("TsUtc", typeof(DateTime));
        table.Columns.Add("Open", typeof(decimal));
        table.Columns.Add("High", typeof(decimal));
        table.Columns.Add("Low", typeof(decimal));
        table.Columns.Add("Close", typeof(decimal));
        table.Columns.Add("Volume", typeof(long));
        table.Columns.Add("Vwap", typeof(decimal));
        table.Columns.Add("TradeCount", typeof(int));

        foreach (var b in bars)
            table.Rows.Add(b.StockId, b.TimeframeId, b.TsUtc, b.Open, b.High, b.Low, b.Close, b.Volume,
                           (object?)b.Vwap ?? DBNull.Value, (object?)b.TradeCount ?? DBNull.Value);

        return new SqlParameter("@Bars", table)
        {
            SqlDbType = SqlDbType.Structured,
            TypeName = "dbo.BarUpsertTvp"
        };
    }
}
