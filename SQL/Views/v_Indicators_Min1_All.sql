/* ======================================================================
   v_EquityIndicators_1m_All — replacement for old v_Indicators_Min1_All
   ====================================================================== */
CREATE OR ALTER VIEW dbo.v_EquityIndicators_1m_All
AS
WITH base AS (
    SELECT
        i.EquityId,
        e.Ticker,
        i.TsUtc,
        -- DST-safe Eastern Time
        i.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time' AS TsET,
        i.MetricCode,
        i.Period,
        i.Value
    FROM dbo.EQUITY_INDICATOR i
    INNER JOIN dbo.EQUITY e ON i.EquityId = e.EquityId
    WHERE i.TimeframeId = 1
      -- Regular trading days only
      AND DATENAME(WEEKDAY, i.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time') NOT IN ('Saturday', 'Sunday')
      -- Regular market hours 9:30 – 16:00 ET
      AND CAST(i.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time' AS time) >= '09:30:00'
      AND CAST(i.TsUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time' AS time) <  '16:00:00'
)
SELECT
    b.Ticker,
    b.EquityId,
    b.TsUtc,
    CAST(b.TsET AS datetime2(0)) AS TsET,
    CONVERT(time(0), b.TsET)     AS EtTime,

    MAX(CASE WHEN b.MetricCode = 'ATS'        AND b.Period = 0   THEN b.Value END) AS ATS,
    MAX(CASE WHEN b.MetricCode = 'ATS_MA_15'  AND b.Period = 15  THEN b.Value END) AS ATS_MA_15,
    MAX(CASE WHEN b.MetricCode = 'ATS_MA_60'  AND b.Period = 60  THEN b.Value END) AS ATS_MA_60,
    MAX(CASE WHEN b.MetricCode = 'ATS_Z_15'   AND b.Period = 15  THEN b.Value END) AS ATS_Z_15,
    MAX(CASE WHEN b.MetricCode = 'ATS_Z_60'   AND b.Period = 60  THEN b.Value END) AS ATS_Z_60,
    MAX(CASE WHEN b.MetricCode = 'IFI_60'     AND b.Period = 60  THEN b.Value END) AS IFI_60,
    MAX(CASE WHEN b.MetricCode = 'RSI'        AND b.Period = 14  THEN b.Value END) AS RSI_14,
    MAX(CASE WHEN b.MetricCode = 'VWAP_DEV'   AND b.Period = 0   THEN b.Value END) AS VWAP_DEV

FROM base b
GROUP BY 
    b.Ticker, 
    b.EquityId, 
    b.TsUtc, 
    b.TsET;
GO

PRINT 'v_EquityIndicators_1m_All deployed — exact replacement for old v_Indicators_Min1_All';