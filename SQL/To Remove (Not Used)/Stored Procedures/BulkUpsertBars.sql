USE [MarketInsight]
GO

/****** Object:  StoredProcedure [dbo].[BulkUpsertBars]    Script Date: 18/11/2025 08:12:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE   PROCEDURE [dbo].[BulkUpsertBars]
    @Bars dbo.BarUpsertTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BAR AS t
    USING @Bars AS s
       ON  t.StockId=s.StockId AND t.TimeframeId=s.TimeframeId AND t.TsUtc=s.TsUtc
    WHEN MATCHED THEN UPDATE SET
         [Open] = s.[Open], [High] = s.[High], [Low] = s.[Low], [Close] = s.[Close],
         Volume = s.Volume, Vwap = s.Vwap, TradeCount = s.TradeCount
    WHEN NOT MATCHED THEN INSERT
        (StockId,TimeframeId,TsUtc,[Open],[High],[Low],[Close],Volume,Vwap,TradeCount)
        VALUES
        (s.StockId,s.TimeframeId,s.TsUtc,s.[Open],s.[High],s.[Low],s.[Close],s.Volume,s.Vwap,s.TradeCount);
END

GO


