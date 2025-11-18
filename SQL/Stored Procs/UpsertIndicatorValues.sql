USE [MarketInsight]
GO

/****** Object:  StoredProcedure [dbo].[UpsertIndicatorValues]    Script Date: 18/11/2025 08:17:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


-- 2) Upsert proc
CREATE   PROCEDURE [dbo].[UpsertIndicatorValues]
    @Rows dbo.IndicatorValueTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.INDICATOR_VALUE WITH (HOLDLOCK) AS t
    USING @Rows AS s
       ON t.StockId     = s.StockId
      AND t.TimeframeId = s.TimeframeId
      AND t.TsUtc       = s.TsUtc
      AND t.MetricCode  = s.MetricCode
      AND ISNULL(t.Period,-1)=ISNULL(s.Period,-1)
    WHEN MATCHED THEN
      UPDATE SET
         t.Value      = s.Value,
         t.ParamsJson = s.ParamsJson,
         t.UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
      INSERT (StockId,TimeframeId,TsUtc,MetricCode,Period,ParamsJson,Value,UpdatedUtc)
      VALUES (s.StockId,s.TimeframeId,s.TsUtc,s.MetricCode,s.Period,s.ParamsJson,s.Value,SYSUTCDATETIME());
END
GO


