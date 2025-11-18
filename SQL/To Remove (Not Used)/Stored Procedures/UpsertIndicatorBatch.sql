USE [MarketInsight]
GO

/****** Object:  StoredProcedure [dbo].[UpsertIndicatorBatch]    Script Date: 18/11/2025 08:24:57 ******/
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


