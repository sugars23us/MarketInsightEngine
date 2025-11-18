USE [MarketInsight]
GO

/****** Object:  View [dbo].[v_LatestIndicators_Min1]    Script Date: 18/11/2025 08:39:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE   VIEW [dbo].[v_LatestIndicators_Min1]
AS
WITH ranked AS (
  SELECT
      iv.StockId,
      s.Ticker,
      iv.TsUtc,
      iv.MetricCode,
      iv.Period,
      iv.Value,
      ROW_NUMBER() OVER (
          PARTITION BY iv.StockId, iv.MetricCode, iv.Period
          ORDER BY iv.TsUtc DESC
      ) AS rn
  FROM dbo.INDICATOR_VALUE iv
  JOIN dbo.STOCK s ON s.StockId = iv.StockId
  WHERE iv.TimeframeId = 1
    -- Regular U.S. session only (DST-safe, New York time)
    AND DATENAME(WEEKDAY, CAST((iv.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time') AS date))
        NOT IN ('Saturday','Sunday')
    AND CAST((iv.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time') AS time) >= '09:30'
    AND CAST((iv.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time') AS time) <  '16:00'
)
SELECT
    Ticker,
    StockId,
    MAX(CASE WHEN MetricCode='ATS'        AND Period=0  THEN Value END) AS ATS,
    MAX(CASE WHEN MetricCode='ATS_MA_15'  AND Period=15 THEN Value END) AS ATS_MA_15,
    MAX(CASE WHEN MetricCode='ATS_MA_60'  AND Period=60 THEN Value END) AS ATS_MA_60,
    MAX(CASE WHEN MetricCode='ATS_Z_15'   AND Period=15 THEN Value END) AS ATS_Z_15,
    MAX(CASE WHEN MetricCode='ATS_Z_60'   AND Period=60 THEN Value END) AS ATS_Z_60,
    MAX(CASE WHEN MetricCode='IFI_60'     AND Period=60 THEN Value END) AS IFI_60,
    MAX(TsUtc) AS TsUtc
FROM ranked
WHERE rn = 1
GROUP BY Ticker, StockId;
GO


