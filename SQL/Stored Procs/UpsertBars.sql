USE [MarketInsight]
GO

/****** Object:  StoredProcedure [dbo].[UpsertBars]    Script Date: 18/11/2025 08:22:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


-- 2) Upsert procedure for BAR
CREATE   PROCEDURE [dbo].[UpsertBars]
    @Rows dbo.BarTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BAR WITH (HOLDLOCK) AS t
    USING @Rows AS s
       ON t.StockId     = s.StockId
      AND t.TimeframeId = s.TimeframeId
      AND t.TsUtc       = s.TsUtc
    WHEN MATCHED THEN
      UPDATE SET
         t.[Open]      = s.[Open],
         t.[High]      = s.[High],
         t.[Low]       = s.[Low],
         t.[Close]     = s.[Close],
         t.Volume      = s.Volume,
         t.Vwap        = s.Vwap,
         t.TradeCount  = s.TradeCount
    WHEN NOT MATCHED THEN
      INSERT (StockId, TimeframeId, TsUtc, [Open], [High], [Low], [Close], Volume, Vwap, TradeCount)
      VALUES (s.StockId, s.TimeframeId, s.TsUtc, s.[Open], s.[High], s.[Low], s.[Close], s.Volume, s.Vwap, s.TradeCount);
END
GO


