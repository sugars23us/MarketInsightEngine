



CREATE OR ALTER PROCEDURE dbo.BulkUpsertBars
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






/* Bulk upsert proc */
CREATE OR ALTER PROCEDURE dbo.BulkUpsertIndicators
    @Indicators dbo.IndicatorUpsertTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.INDICATOR_VALUE AS t
    USING @Indicators AS s
       ON t.StockId=s.StockId
      AND t.TimeframeId=s.TimeframeId
      AND t.TsUtc=s.TsUtc
      AND t.MetricCode=s.MetricCode
      AND ISNULL(t.Period,-1)=ISNULL(s.Period,-1)
    WHEN MATCHED THEN
      UPDATE SET t.Value=s.Value, t.ParamsJson=s.ParamsJson, t.UpdatedUtc=SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
      INSERT (StockId,TimeframeId,TsUtc,MetricCode,Period,ParamsJson,Value,UpdatedUtc)
      VALUES (s.StockId,s.TimeframeId,s.TsUtc,s.MetricCode,s.Period,s.ParamsJson,s.Value,SYSUTCDATETIME());
END
GO



USE [MarketInsight]
GO

/****** Object:  StoredProcedure [dbo].[UpsertIndicatorBatch]    Script Date: 10/11/2025 10:05:20 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[UpsertIndicatorBatch]
    @Indicators dbo.Indicator_TVP READONLY
AS
BEGIN
  SET NOCOUNT ON;

  MERGE dbo.INDICATOR_VALUE AS t
  USING @Indicators AS s
     ON t.StockId     = s.StockId
    AND t.TimeframeId = s.TimeframeId
    AND t.TsUtc       = s.TsUtc
    AND t.MetricCode  = s.MetricCode
  WHEN MATCHED THEN
    UPDATE SET
      t.Value      = s.Value,
      t.ParamsJson = s.ParamsJson,
      t.UpdatedUtc = SYSUTCDATETIME(),
      t.Period     = s.Period
  WHEN NOT MATCHED THEN
    INSERT (StockId,TimeframeId,TsUtc,MetricCode,Period,ParamsJson,Value,UpdatedUtc)
    VALUES (s.StockId,s.TimeframeId,s.TsUtc,s.MetricCode,s.Period,s.ParamsJson,s.Value,SYSUTCDATETIME());
END
GO











